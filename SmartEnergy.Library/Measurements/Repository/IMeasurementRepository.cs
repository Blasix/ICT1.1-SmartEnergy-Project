using SmartEnergy.Library.Measurements.Models;

namespace SmartEnergy.Library.Measurements.Repository;

public interface IMeasurementRepository
{
    /// <summary>
    /// Retrieves a list of measurement objects that represent the P1 Meter Values regarding the consumption of energy.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregationWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    Task<List<Measurement>> GetEnergyConsumed(int meterId, int daysToRetrieve, string aggregationWindow);

    /// <summary>
    /// Retrieves a list of measurement objects that represent the P1 Meter Values regarding the production of energy. The results
    /// will only yield a (positve) value in case power was produced at the site at any point during the lifespan of the energy meter.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregationWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    Task<List<Measurement>> GetEnergyProduced(int meterId, int daysToRetrieve, string aggregationWindow);

    /// <summary>
    /// Retrieves a list of measurement objects that represent the P1 Meter Values regarding the consumption of gas.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregationWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    Task<List<Measurement>> GetGasDelivered(int meterId, int daysToRetrieve, string aggregationWindow);

    /// <summary>
    /// Retrieves a list of measurement objects that represent the average power usage for the given aggregation window. The power
    /// usage will be positve if power is drawn from the energy provided and will be negative in case power is produced and is fed
    /// back to the energy provided (i.e. when using solar panels or local battery discharge)
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregationWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    Task<List<Measurement>> GetPower(int meterId, int daysToRetrieve, string aggregationWindow);
}