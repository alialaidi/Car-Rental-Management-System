using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class VehicleImageRepository(SqlExecutor executor) : IVehicleImageRepository
{
    public Task<int> AddAsync(VehicleImage image) => Run(() => executor.ExecuteScalarInt32Async("usp_VehicleImages_Add",p=>
    { AddInt(p,"@VehicleId",image.VehicleId);p.Add("@ImageUrl",SqlDbType.NVarChar,500).Value=image.ImageUrl;p.Add("@IsPrimary",SqlDbType.Bit).Value=image.IsPrimary; }));
    
    public Task<IReadOnlyList<VehicleImage>> ListByVehicleAsync(int id) => executor.QueryAsync("usp_VehicleImages_ListByVehicle", Map, p=>AddInt(p,"@VehicleId",id));
    
    public Task RemoveAsync(int id) => Run(()=>executor.ExecuteNonQueryAsync("usp_VehicleImages_Remove",p=>AddInt(p,"@ImageId",id)));
    
    public Task SetPrimaryAsync(int vehicleId,int imageId)=>Run(()=>executor.ExecuteNonQueryAsync("usp_VehicleImages_SetPrimary",p=>{AddInt(p,"@VehicleId",vehicleId);AddInt(p,"@ImageId",imageId);}));
    
    private static VehicleImage Map(SqlDataReader r)=>new(){ImageId=r.GetInt32(r.GetOrdinal("ImageId")),VehicleId=r.GetInt32(r.GetOrdinal("VehicleId")),ImageUrl=r.GetString(r.GetOrdinal("ImageUrl")),IsPrimary=r.GetBoolean(r.GetOrdinal("IsPrimary"))};
    
    private static async Task<T> Run<T>(Func<Task<T>> action){try{return await action();}catch(SqlException ex){SqlErrorTranslator.ThrowIfKnown(ex);throw;}}
    
    private static async Task Run(Func<Task> action){try{await action();}catch(SqlException ex){SqlErrorTranslator.ThrowIfKnown(ex);throw;}}
    
    private static void AddInt(SqlParameterCollection p,string n,int v)=>p.Add(n,SqlDbType.Int).Value=v;


}
