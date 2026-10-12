namespace _CarRental.Application.DTOs.Vehicles;
public class CreateVehicleRequest
{
    public string PlateNumber { get; set; } = string.Empty; public string Make { get; set; } = string.Empty; public string Model { get; set; } = string.Empty;
    public int Year { get; set; } public int CategoryId { get; set; } public int FuelTypeId { get; set; } public int TransmissionTypeId { get; set; }
    public int SeatsCount { get; set; } public decimal DailyPrice { get; set; } public int BranchId { get; set; } public int CurrentMileage { get; set; }
}
public sealed class UpdateVehicleRequest : CreateVehicleRequest { }
public sealed class AddImageRequest { public string ImageUrl { get; set; } = string.Empty; public bool IsPrimary { get; set; } }
public sealed record VehicleDto(int VehicleId,string PlateNumber,string Make,string Model,int Year,int CategoryId,int FuelTypeId,int TransmissionTypeId,int SeatsCount,decimal DailyPrice,int BranchId,string Status,int CurrentMileage);
public sealed record VehicleImageDto(int ImageId,int VehicleId,string ImageUrl,bool IsPrimary);
