namespace _CarRental.Application.DTOs.Vehicles;

public sealed record VehicleListItemDto(
    int VehicleId,
    string Make,
    string Model,
    int Year,
    int CategoryId,
    string CategoryName,
    int FuelTypeId,
    string FuelTypeName,
    int TransmissionTypeId,
    string TransmissionTypeName,
    int SeatsCount,
    decimal DailyPrice,
    int BranchId,
    string Status,
    string? PrimaryImageUrl);
