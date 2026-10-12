using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class LookupRepository(SqlExecutor executor) : ILookupRepository
{
    public Task<IReadOnlyList<LookupItem>> ListAsync(VehicleLookupType type) => executor.QueryAsync("usp_Lookups_List", Map,
        p=>p.Add("@Type",SqlDbType.NVarChar,30).Value=TypeName(type));
    public Task<int> CreateAsync(VehicleLookupType type,string name,string? description) => Run(()=>executor.ExecuteScalarInt32Async("usp_Lookups_Create",p=>Configure(p,type,name,description)));
    public Task UpdateAsync(VehicleLookupType type,int id,string name,string? description) => Run(()=>executor.ExecuteNonQueryAsync("usp_Lookups_Update",p=>{Configure(p,type,name,description);p.Add("@Id",SqlDbType.Int).Value=id;}));
    public Task DeleteAsync(VehicleLookupType type,int id) => Run(()=>executor.ExecuteNonQueryAsync("usp_Lookups_Delete",p=>{p.Add("@Type",SqlDbType.NVarChar,30).Value=TypeName(type);p.Add("@Id",SqlDbType.Int).Value=id;}));
    private static LookupItem Map(SqlDataReader r){var d=r.GetOrdinal("Description");return new LookupItem(r.GetInt32(r.GetOrdinal("Id")),r.GetString(r.GetOrdinal("Name")),r.IsDBNull(d)?null:r.GetString(d));}
    private static void Configure(SqlParameterCollection p,VehicleLookupType type,string name,string? description)
    {p.Add("@Type",SqlDbType.NVarChar,30).Value=TypeName(type);p.Add("@Name",SqlDbType.NVarChar,100).Value=name;p.Add("@Description",SqlDbType.NVarChar,500).Value=(object?)description??DBNull.Value;}
    private static string TypeName(VehicleLookupType type)=>type switch{VehicleLookupType.Category=>"Category",VehicleLookupType.FuelType=>"FuelType",VehicleLookupType.TransmissionType=>"TransmissionType",_=>throw new ArgumentOutOfRangeException(nameof(type))};
    private static async Task<T> Run<T>(Func<Task<T>> action){try{return await action();}catch(SqlException ex){SqlErrorTranslator.ThrowIfKnown(ex);throw;}}
    private static async Task Run(Func<Task> action){try{await action();}catch(SqlException ex){SqlErrorTranslator.ThrowIfKnown(ex);throw;}}
}
