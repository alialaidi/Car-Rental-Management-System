namespace _CarRental.Application.DTOs.Vehicles;

public sealed record VehicleDetailsDto(
    int VehicleId,
    string PlateNumber,
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
    int CurrentMileage,
    IReadOnlyList<VehicleImageDto> Images);
