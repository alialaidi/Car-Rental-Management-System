using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Application.Validators;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.ValueObjects;

namespace _CarRental.Application.Services;

public sealed class VehicleQueryService(
    IUnitOfWork unitOfWork,
    VehicleSearchRequestValidator validator) : IVehicleQueryService
{
    public async Task<PagedResult<VehicleListItemDto>> SearchAsync(VehicleSearchRequest request)
    {
        var normalized = validator.Validate(request);
        var page = await unitOfWork.Vehicles.SearchAvailableAsync(new VehicleSearchCriteria(
            normalized.CategoryId,
            normalized.FuelTypeId,
            normalized.TransmissionTypeId,
            normalized.BranchId,
            normalized.MinimumDailyPrice,
            normalized.MaximumDailyPrice,
            normalized.MinimumSeats,
            normalized.PickupDateTime?.UtcDateTime,
            normalized.ExpectedReturnDateTime?.UtcDateTime,
            normalized.Page,
            normalized.PageSize,
            normalized.SortBy!,
            normalized.SortDirection!));

        return new PagedResult<VehicleListItemDto>(
            page.Items.Select(ToListItem).ToArray(),
            normalized.Page,
            normalized.PageSize,
            page.TotalCount);
    }

    public async Task<VehicleDetailsDto> GetDetailsAsync(int vehicleId)
    {
        if (vehicleId <= 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "VehicleId must be positive.");

        var details = await unitOfWork.Vehicles.GetCatalogDetailsAsync(vehicleId)
            ?? throw new NotFoundException("Vehicle was not found.");
        var item = details.CatalogItem;
        var vehicle = item.Vehicle;
        return new VehicleDetailsDto(
            vehicle.VehicleId,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.CategoryId,
            item.CategoryName,
            vehicle.FuelTypeId,
            item.FuelTypeName,
            vehicle.TransmissionTypeId,
            item.TransmissionTypeName,
            vehicle.SeatsCount,
            vehicle.DailyPrice,
            vehicle.BranchId,
            vehicle.Status,
            vehicle.CurrentMileage,
            details.Images.Select(image => new VehicleImageDto(image.ImageId, image.VehicleId, image.ImageUrl, image.IsPrimary)).ToArray());
    }

    private static VehicleListItemDto ToListItem(VehicleCatalogItem item)
    {
        var vehicle = item.Vehicle;
        return new VehicleListItemDto(
            vehicle.VehicleId, vehicle.Make, vehicle.Model, vehicle.Year,
            vehicle.CategoryId, item.CategoryName,
            vehicle.FuelTypeId, item.FuelTypeName,
            vehicle.TransmissionTypeId, item.TransmissionTypeName,
            vehicle.SeatsCount, vehicle.DailyPrice, vehicle.BranchId, vehicle.Status,
            item.PrimaryImageUrl);
    }
}
