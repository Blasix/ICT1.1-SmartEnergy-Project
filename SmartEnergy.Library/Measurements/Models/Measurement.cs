namespace SmartEnergy.Library.Measurements.Models;

public record Measurement(DateTime Timestamp,
                          string LocationId,
                          Sensor Sensor,
                          double? Value,
                          Unit Unit,
                          double? EnergyPrice,
                          double? Temperature);

