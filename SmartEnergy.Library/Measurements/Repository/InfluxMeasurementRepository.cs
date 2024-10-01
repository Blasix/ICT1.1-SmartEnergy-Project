using System.Diagnostics;
using InfluxDB.Client;
using SmartEnergy.Library.Measurements.Models;

namespace SmartEnergy.Library.Measurements.Repository;

/// <summary>
/// Implementation of the <c>IMeasurementRepository<c> interface.
/// </summary>
public class InfluxMeasurementRepository : IMeasurementRepository
{
    private readonly IInfluxDBClient _client;

    public InfluxMeasurementRepository(IInfluxDBClient client)
    {
        _client = client;
    }

    public Task<List<Measurement>> GetEnergyConsumed(int meterId, int daysToRetrieve, string aggregationWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryP1SmartMeter(meterId, daysToRetrieve, aggregationWindow, Sensor.energy_consumed, Unit.KilowattHour);
    }

    public Task<List<Measurement>> GetEnergyProduced(int meterId, int daysToRetrieve, string aggregationWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryP1SmartMeter(meterId, daysToRetrieve, aggregationWindow, Sensor.energy_produced, Unit.KilowattHour);
    }

    public Task<List<Measurement>> GetGasDelivered(int meterId, int daysToRetrieve, string aggregationWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryP1SmartMeter(meterId, daysToRetrieve, aggregationWindow, Sensor.gas_delivered, Unit.CubicMeter);
    }

    public async Task<List<Measurement>> GetPower(int meterId, int daysToRetrieve, string aggregationWindow)
    {
        var measurements = new List<Measurement>();
        // A stopwatch is used so we can monitor the time it took to retrieve and process the data from the influx database
        var startTime = Stopwatch.StartNew();

        // This is an influx query. Influx processes this and returns the data based on the parameter you provide in the query
        string query =
            $"from(bucket: \"p1-smartmeters\")" +
            $"  |> range(start: {getStartDate(daysToRetrieve)}, stop: now())" +
            $"  |> filter(fn: (r) => r[\"signature\"] == \"{getFullMeterIdInHex(meterId)}\")" +
            $"  |> filter(fn: (r) => r[\"_field\"] == \"power_consumed\" or r[\"_field\"] == \"power_produced\")" +
            $"  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")" +
            $"  |> map(fn: (r) => ({{r with _value: r.power_consumed - r.power_produced}}))" +
            $"  |> aggregateWindow(every: {aggregationWindow}, fn: mean, createEmpty: false)" +
            $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // retrieve the temperature and price data to enrich measurements with this information
        var energyPrices = await EnrichMeasurementsWithEnergyPrice(daysToRetrieve);
        var temperatures = await EnrichMeasurementsWithTemperature(daysToRetrieve, aggregationWindow);

        // Make an API Call to the Influx server to retrieve the data requested by the query
        var fluxTables = await _client.GetQueryApi().QueryAsync(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to Measurement object and stored in the measurements list.
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                // Get the right fields we need from the record and store it in a local variable for later use
                DateTime dateTime = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc();
                string locationId = (string)record.GetValueByKey("signature");
                double value = (double)record.GetValueByKey("_value") * 1000;

                // round up to the full hour so we can lookup the energy price for the hourly slot
                DateTime dateTimeRounded = dateTime.AddMinutes(dateTime.Minute * -1).AddSeconds(dateTime.Second * -1);

                // We use the earlier fetched data to set energyprice and temperature on the retrieved date/time of the measurement data
                energyPrices.TryGetValue(dateTimeRounded.Ticks, out var energyPrice);
                temperatures.TryGetValue(dateTime.Ticks, out var temperature);

                // Create a new Measurement object and add it to the list.
                var singleMeasurement = new Measurement(dateTime, locationId, Sensor.power, Math.Round(value, 0), Unit.Watt, energyPrice, temperature);
                measurements.Add(singleMeasurement);
            }
        }

        // Print the time it has taken to query and process the data before returning the list of measurements
        Console.WriteLine("Time consumed for API Call: " + startTime.ElapsedMilliseconds + "ms");
        return measurements;
    }

    /// <summary>
    /// Shared method with common implementation for querying the influx database.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregationWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    /// <param name="sensor">Type of data to retrieve from the influx database</param>
    /// <param name="unit">The unit of representation that is linked to the <c>sensor</c> information</param>
    /// <returns></returns>
    private async Task<List<Measurement>> QueryP1SmartMeter(int meterId, int daysToRetrieve, string aggregationWindow, Sensor sensor, Unit unit)
    {
        var measurements = new List<Measurement>();
        // A stopwatch is used so we can monitor the time it took to retrieve and process the data from the influx database
        var startTime = Stopwatch.StartNew();

        // This is an influx query. Influx processes this and returns the data based on the parameter you provide in the query
        string query =
            $"from(bucket: \"p1-smartmeters\")" +
            $"  |> range(start: {getStartDate(daysToRetrieve)}, stop: now())" +
            $"  |> filter(fn: (r) => r[\"_field\"] == \"{sensor}\")" +
            $"  |> filter(fn: (r) => r[\"signature\"] == \"{getFullMeterIdInHex(meterId)}\")" +
            $"  |> aggregateWindow(every: {aggregationWindow}, fn: min, createEmpty: false)" +
            $"  |> yield(name: \"min\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // in case of energy consumed or produced we will enrich the measurements with the power prices
        var energyPrices = new Dictionary<long, double>();
        if (Sensor.energy_consumed.Equals(sensor) || Sensor.energy_produced.Equals(sensor))
        {
            energyPrices = await EnrichMeasurementsWithEnergyPrice(daysToRetrieve);
        }

        // enrich measurements with the temperature data available, independend of the type of sensor requested
        var temperatures = await EnrichMeasurementsWithTemperature(daysToRetrieve, aggregationWindow);

        // Make an API Call to the Influx server to retrieve the data requested by the query
        var fluxTables = await _client.GetQueryApi().QueryAsync(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to Measurement object and stored in the measurements list.
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                // Get the right fields we need from the record and store it in a local variable for later use
                DateTime dateTime = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc();
                string locationId = (string)record.GetValueByKey("signature");
                double value = (double)record.GetValueByKey("_value");

                // Round the value up to the standarized rounding for measurements:
                // * gas will be rounded to three decimals
                // * energy will be rounded to full numbers (no decimals)
                if (Sensor.gas_delivered.Equals(sensor))
                {
                    value = Math.Round(value, 3);
                }
                else
                {
                    value = Math.Round(value, 0);
                }

                // round up to the full hour so we can lookup the energy price for the hourly slot
                DateTime dateTimeRounded = dateTime.AddMinutes(dateTime.Minute * -1).AddSeconds(dateTime.Second * -1);

                // We use the earlier fetched data to set energyprice and temperature on the retrieved date/time of the measurement data
                energyPrices.TryGetValue(dateTimeRounded.Ticks, out var energyPrice);
                temperatures.TryGetValue(dateTime.Ticks, out var temperature);

                // Create a new Measurement object and add it to the list.
                var singleMeasurement = new Measurement(dateTime, locationId, sensor, value, unit, energyPrice, temperature);
                measurements.Add(singleMeasurement);
            }
        }

        // Print the time it has taken to query and process the data before returning the list of measurements
        Console.WriteLine("Time consumed for API Call: " + startTime.ElapsedMilliseconds + "ms");
        return measurements;
    }

    /// <summary>
    /// Method to retrieve the temperature from the database. The temperature is published every hour and we will
    /// interpolate te values based on the value of AGGREGATE_WINDOW_TIME.
    ///
    /// The getStartDate is manipulated a bit to retrieve an extra day. Otherwise the measurement records will have no value for
    /// temperature for the first hour of the day (12:00AM till 1:00AM). Note: 12:00AM is considered to be part of the day before.
    /// </summary>
    private async Task<Dictionary<long, double>> EnrichMeasurementsWithTemperature(int daysToRetrieve, string aggregationWindow)
    {
        var temperatures = new Dictionary<long, double>();
        string query =
                $"import \"interpolate\"" +
                $"from(bucket: \"ha-playground\")" +
                $"  |> range(start: {getStartDate(daysToRetrieve + 1)}, stop: now())" +
                $"  |> filter(fn: (r) => r[\"entity_id\"] == \"forecast_sendlab_playground\")" +
                $"  |> filter(fn: (r) => r[\"domain\"] == \"weather\")" +
                $"  |> filter(fn: (r) => r[\"_field\"] == \"temperature\")" +
                $"  |> interpolate.linear(every: {aggregationWindow})" +
                $"  |> aggregateWindow(every: {aggregationWindow}, fn: mean, createEmpty: false)" +
                $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to key (time) and value (temperature) pairs and stored in the energyPrices list.
        var fluxTables = await _client.GetQueryApi().QueryAsync(query);
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                long ticks = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc().Ticks;
                double temperature = (double)record.GetValueByKey("_value");
                temperatures.Add(ticks, Math.Round(temperature, 1));
            }
        }

        // return the list of key value pairs
        return temperatures;
    }

    /// <summary>
    /// Method to retrieve the energy prices from the database in hourly windows
    ///
    /// The getStartDate is manipulated a bit to retrieve an extra day. Otherwise the measurement records will have no value for
    /// temperature for the first hour of the day (12:00AM till 1:00AM). Note: 12:00AM is considered to be part of the day before.
    /// </summary>
    private async Task<Dictionary<long, double>> EnrichMeasurementsWithEnergyPrice(int daysToRetrieve)
    {
        var energyPrices = new Dictionary<long, double>();
        string query =
                $"from(bucket: \"ha-playground\")" +
                $"  |> range(start: {getStartDate(daysToRetrieve + 1)}, stop: now())" +
                $"  |> filter(fn: (r) => r[\"entity_id\"] == \"nordpool\")" +
                $"  |> filter(fn: (r) => r[\"_field\"] == \"value\")" +
                $"  |> aggregateWindow(every: 60m, fn: mean, createEmpty: false)" + /// hardcoded aggregationWindow, since that is the price window (per hour)
                $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTables. These records
        // are then converted to key (time) and value (price) pairs and stored in the energyPrices list.
        var fluxTables = await _client.GetQueryApi().QueryAsync(query);
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                long ticks = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc().Ticks;
                double energyPrice = (double)record.GetValueByKey("_value");
                energyPrices.Add(ticks, Math.Round(energyPrice, 4));
            }
        }

        /// return the list of key value pairs
        return energyPrices;
    }

    /// <summary>
    /// A method to determine the earliest date based on the parameter lastNumberOfDaysToRetrieve
    /// </summary>
    private static string getStartDate(int lastNumberOfDaysToRetrieve)
    {
        // to prevent the database from overloading the maximum number of days will be 30
        lastNumberOfDaysToRetrieve = Math.Min(30, lastNumberOfDaysToRetrieve);

        // subtract 1 day to get the correct start date. From the user perspective it is more logical to
        // assume that one day is today, but the days to subtract should then be 0, thus the correction.
        return DateTime.Now.AddDays((lastNumberOfDaysToRetrieve - 1) * -1).ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Returns the full the Meter identifier (id) based on the provided meterId
    /// </summary>
    private string getFullMeterIdInHex(int meterId)
    {
        return "2019-ETI-EMON-V01-" + meterId.ToString("X") + "-16405E";
    }
}