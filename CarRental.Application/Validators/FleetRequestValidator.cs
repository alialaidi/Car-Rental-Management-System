using System.Globalization;
using _CarRental.Application.DTOs.Lookups;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public static class FleetRequestValidator
{
    public static LookupRequest Validate(LookupRequest request,VehicleLookupType type)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Name=Required(request.Name,type==VehicleLookupType.Category?100:50,"Name");
        request.Description=request.Description?.Trim();
        if(type!=VehicleLookupType.Category) request.Description=null;
        if(request.Description?.Length>500) throw new BusinessRuleException("Description must not exceed 500 characters.");
        return request;
    }

    public static CreateVehicleRequest Validate(CreateVehicleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.PlateNumber=Required(request.PlateNumber,20,"PlateNumber").ToUpperInvariant();
        request.Make=Required(request.Make,100,"Make"); request.Model=Required(request.Model,100,"Model");
        if(request.Year<1990) throw new BusinessRuleException("Year must be at least 1990.");
        if(request.CategoryId<=0||request.FuelTypeId<=0||request.TransmissionTypeId<=0||request.BranchId<=0) throw new BusinessRuleException("Lookup and branch ids must be positive.");
        if(request.SeatsCount<=0) throw new BusinessRuleException("SeatsCount must be positive.");
        if(request.DailyPrice<0||request.DailyPrice>9999999999999999.99m||decimal.Round(request.DailyPrice,2)!=request.DailyPrice)
            throw new BusinessRuleException("DailyPrice must be non-negative and have at most two decimal places.");
        if(request.CurrentMileage<0) throw new BusinessRuleException("CurrentMileage cannot be negative.");
        return request;
    }

    public static string ValidateImageUrl(string? value)
    {
        var url=value?.Trim();
        if(string.IsNullOrWhiteSpace(url)||url.Length>500||!Uri.TryCreate(url,UriKind.Absolute,out var parsed)||
           (parsed.Scheme!=Uri.UriSchemeHttp&&parsed.Scheme!=Uri.UriSchemeHttps))
            throw new BusinessRuleException("ImageUrl must be an absolute HTTP or HTTPS URL up to 500 characters.");
        return url;
    }

    private static string Required(string? value,int length,string name)
    {
        var normalized=value?.Trim()??string.Empty;
        if(normalized.Length==0||normalized.Length>length) throw new BusinessRuleException($"{name} is required and cannot exceed {length} characters.");
        return normalized;
    }


}
