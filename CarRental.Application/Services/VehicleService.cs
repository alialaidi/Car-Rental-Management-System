using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Common.Security;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class VehicleService(IUnitOfWork unitOfWork,ICurrentUser currentUser,IAuditLogger auditLogger):IVehicleService
{
    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        FleetRequestValidator.Validate(request);
        await EnsureBranchScopeAsync(request.BranchId);
        await EnsureReferencesAsync(request);
        var vehicle=Map(request);
        vehicle.VehicleId=await unitOfWork.Vehicles.CreateAsync(vehicle);
        await AuditAsync(AuditActions.VehicleCreated,vehicle.VehicleId,null,ToDto(vehicle));
        return ToDto(vehicle);
    }

    public async Task<VehicleDto> GetAsync(int id)
    {
        var vehicle=await RequireVehicleAsync(id);
        await EnsureBranchScopeAsync(vehicle.BranchId);
        return ToDto(vehicle);
    }

    public async Task<PagedResult<VehicleDto>> ListByBranchAsync(int branchId,PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureBranchScopeAsync(branchId);
        if(await unitOfWork.Branches.GetByIdAsync(branchId)is null)throw new NotFoundException("Branch was not found.");
        var vehicles=await unitOfWork.Vehicles.ListByBranchAsync(branchId,request.Page,request.PageSize);
        return new PagedResult<VehicleDto>(vehicles.Select(ToDto).ToArray(),request.Page,request.PageSize,await unitOfWork.Vehicles.GetCountByBranchAsync(branchId));
    }

    public async Task<VehicleDto> UpdateAsync(int id,UpdateVehicleRequest request)
    {
        FleetRequestValidator.Validate(request);
        var vehicle=await RequireVehicleAsync(id);
        await EnsureBranchScopeAsync(vehicle.BranchId);await EnsureBranchScopeAsync(request.BranchId);await EnsureReferencesAsync(request);
        var old=ToDto(vehicle);
        vehicle.PlateNumber=request.PlateNumber;vehicle.Make=request.Make;vehicle.Model=request.Model;vehicle.Year=request.Year;
        vehicle.CategoryId=request.CategoryId;vehicle.FuelTypeId=request.FuelTypeId;vehicle.TransmissionTypeId=request.TransmissionTypeId;
        vehicle.SeatsCount=request.SeatsCount;vehicle.DailyPrice=request.DailyPrice;vehicle.BranchId=request.BranchId;vehicle.CurrentMileage=request.CurrentMileage;
        await unitOfWork.Vehicles.UpdateAsync(vehicle);
        var updated=ToDto(vehicle);await AuditAsync(AuditActions.VehicleUpdated,id,old,updated);return updated;
    }

    public async Task DeleteAsync(int id)
    {
        var vehicle=await RequireVehicleAsync(id);await EnsureBranchScopeAsync(vehicle.BranchId);
        await unitOfWork.Vehicles.DeleteAsync(id);
        await AuditAsync(AuditActions.VehicleDeleted,id,ToDto(vehicle),new{Result="deleted"});
    }

    public async Task<IReadOnlyList<VehicleImageDto>> ListImagesAsync(int vehicleId)
    {
        var vehicle=await RequireVehicleAsync(vehicleId);await EnsureBranchScopeAsync(vehicle.BranchId);
        return (await unitOfWork.VehicleImages.ListByVehicleAsync(vehicleId)).Select(ToDto).ToArray();
    }

    public async Task<VehicleImageDto> AddImageAsync(int vehicleId,AddImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var vehicle=await RequireVehicleAsync(vehicleId);await EnsureBranchScopeAsync(vehicle.BranchId);
        var image=new VehicleImage{VehicleId=vehicleId,ImageUrl=FleetRequestValidator.ValidateImageUrl(request.ImageUrl),IsPrimary=request.IsPrimary};
        image.ImageId=await unitOfWork.VehicleImages.AddAsync(image);
        await AuditAsync(AuditActions.VehicleImageChanged,vehicleId,null,new{image.ImageId,image.ImageUrl,image.IsPrimary});
        return ToDto(image);
    }

    public async Task RemoveImageAsync(int vehicleId,int imageId)
    {
        var vehicle=await RequireVehicleAsync(vehicleId);await EnsureBranchScopeAsync(vehicle.BranchId);
        var image=(await unitOfWork.VehicleImages.ListByVehicleAsync(vehicleId)).FirstOrDefault(x=>x.ImageId==imageId)
            ??throw new NotFoundException("Vehicle image was not found.");
        await unitOfWork.VehicleImages.RemoveAsync(imageId);
        await AuditAsync(AuditActions.VehicleImageChanged,vehicleId,new{image.ImageId,image.ImageUrl,image.IsPrimary},new{Result="removed"});
    }

    public async Task SetPrimaryImageAsync(int vehicleId,int imageId)
    {
        var vehicle=await RequireVehicleAsync(vehicleId);await EnsureBranchScopeAsync(vehicle.BranchId);
        var image=(await unitOfWork.VehicleImages.ListByVehicleAsync(vehicleId)).FirstOrDefault(x=>x.ImageId==imageId)
            ??throw new NotFoundException("Vehicle image was not found.");
        await unitOfWork.VehicleImages.SetPrimaryAsync(vehicleId,imageId);
        await AuditAsync(AuditActions.VehicleImageChanged,vehicleId,new{image.ImageId,Primary=false},new{image.ImageId,Primary=true});
    }

    private async Task EnsureBranchScopeAsync(int branchId)
    {
        if(currentUser.IsInRole(RoleNames.Admin))return;
        if(!currentUser.IsInRole(RoleNames.BranchManager))throw new ForbiddenException("Only administrators and branch managers can manage vehicles.");
        await new OwnershipGuard(currentUser).EnsureBranchAsync(branchId);
    }

    private async Task EnsureReferencesAsync(CreateVehicleRequest request)
    {
        foreach(var pair in new[]{(VehicleLookupType.Category,request.CategoryId),(VehicleLookupType.FuelType,request.FuelTypeId),(VehicleLookupType.TransmissionType,request.TransmissionTypeId)})
            if(!(await unitOfWork.Lookups.ListAsync(pair.Item1)).Any(item=>item.Id==pair.Item2))throw new NotFoundException($"{pair.Item1} lookup was not found.");
        if(await unitOfWork.Branches.GetByIdAsync(request.BranchId)is null)throw new NotFoundException("Branch was not found.");
    }

    private async Task<Vehicle> RequireVehicleAsync(int id)=>await unitOfWork.Vehicles.GetByIdAsync(id)??throw new NotFoundException("Vehicle was not found.");
    
    private Task AuditAsync(string action,int id,object? oldValue,object? newValue)=>auditLogger.LogAsync(action,"Vehicles",id.ToString(CultureInfo.InvariantCulture),oldValue,newValue,currentUser.UserId);
    
    private static Vehicle Map(CreateVehicleRequest r)=>new(){PlateNumber=r.PlateNumber,Make=r.Make,Model=r.Model,Year=r.Year,CategoryId=r.CategoryId,FuelTypeId=r.FuelTypeId,TransmissionTypeId=r.TransmissionTypeId,SeatsCount=r.SeatsCount,DailyPrice=r.DailyPrice,BranchId=r.BranchId,CurrentMileage=r.CurrentMileage,Status=VehicleStatus.Available.ToString()};
    
    private static VehicleDto ToDto(Vehicle v)=>new(v.VehicleId,v.PlateNumber,v.Make,v.Model,v.Year,v.CategoryId,v.FuelTypeId,v.TransmissionTypeId,v.SeatsCount,v.DailyPrice,v.BranchId,v.Status,v.CurrentMileage);
    
    private static VehicleImageDto ToDto(VehicleImage i)=>new(i.ImageId,i.VehicleId,i.ImageUrl,i.IsPrimary);

}
