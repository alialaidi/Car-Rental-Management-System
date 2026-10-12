namespace _CarRental.Domain.Entities;
public sealed class Vehicle
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int CategoryId { get; set; }
    public int FuelTypeId { get; set; }
    public int TransmissionTypeId { get; set; }
    public int SeatsCount { get; set; }
    public decimal DailyPrice { get; set; }
    public int BranchId { get; set; }
    public string Status { get; set; } = _CarRental.Domain.Enums.VehicleStatus.Available.ToString();
    public int CurrentMileage { get; set; }
}
