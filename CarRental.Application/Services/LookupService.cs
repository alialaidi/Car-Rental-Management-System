using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Lookups;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class LookupService(IUnitOfWork unitOfWork,ICurrentUser currentUser,IAuditLogger auditLogger):ILookupService
{
    public async Task<IReadOnlyList<LookupDto>> ListAsync(VehicleLookupType type)=>
        (await unitOfWork.Lookups.ListAsync(type)).Select(x=>new LookupDto(x.Id,x.Name,x.Description)).ToArray();

    public async Task<LookupDto> CreateAsync(VehicleLookupType type,LookupRequest request)
    {
        FleetRequestValidator.Validate(request,type);
        var id=await unitOfWork.Lookups.CreateAsync(type,request.Name,request.Description);
        await AuditAsync(type,id,new{request.Name,request.Description});
        return new LookupDto(id,request.Name,request.Description);
    }

    public async Task<LookupDto> UpdateAsync(VehicleLookupType type,int id,LookupRequest request)
    {
        FleetRequestValidator.Validate(request,type);
        var previous=(await unitOfWork.Lookups.ListAsync(type)).FirstOrDefault(x=>x.Id==id)
            ??throw new NotFoundException("Lookup was not found.");
        await unitOfWork.Lookups.UpdateAsync(type,id,request.Name,request.Description);
        await AuditAsync(type,id,new{request.Name,request.Description});
        return new LookupDto(id,request.Name,request.Description);
    }

    public async Task DeleteAsync(VehicleLookupType type,int id)
    {
        await unitOfWork.Lookups.DeleteAsync(type,id);
        await AuditAsync(type,id,new{Result="deleted"});
    }

    private Task AuditAsync(VehicleLookupType type,int id,object value)=>auditLogger.LogAsync(AuditActions.LookupChanged,
        type.ToString(),id.ToString(CultureInfo.InvariantCulture),null,value,currentUser.UserId);


}
