using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using _CarRental.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class VehicleRepository(SqlExecutor executor) : IVehicleRepository
{
    public Task<int> CreateAsync(Vehicle vehicle) => Run(() => executor.ExecuteScalarInt32Async("usp_Vehicles_Create", p => Configure(p, vehicle)));
    public Task<Vehicle?> GetByIdAsync(int id) => executor.QuerySingleOrDefaultAsync("usp_Vehicles_GetById", VehicleMapper.Map, p => AddInt(p,"@VehicleId",id));
    public Task<IReadOnlyList<Vehicle>> ListByBranchAsync(int branchId,int page,int pageSize) => executor.QueryAsync("usp_Vehicles_ListByBranch",VehicleMapper.Map,p=>{AddInt(p,"@BranchId",branchId);AddInt(p,"@Page",page);AddInt(p,"@PageSize",pageSize);});
    public Task<int> GetCountByBranchAsync(int branchId) => executor.ExecuteScalarInt32Async("usp_Vehicles_CountByBranch",p=>AddInt(p,"@BranchId",branchId));
    public async Task<VehicleCatalogPage> SearchAvailableAsync(VehicleSearchCriteria criteria)
    {
        var (counts, items) = await executor.QueryTwoResultSetsAsync(
            "usp_Vehicles_Search",
            reader => reader.GetInt32(reader.GetOrdinal("TotalCount")),
            MapCatalogItem,
            p => ConfigureSearch(p, criteria));
        return new VehicleCatalogPage(items, counts.FirstOrDefault());
    }

    public async Task<VehicleCatalogDetails?> GetCatalogDetailsAsync(int vehicleId)
    {
        var rows = await executor.QueryAsync("usp_Vehicles_GetCatalogDetails", MapCatalogDetailsRow,
            p => AddInt(p, "@VehicleId", vehicleId));
        if (rows.Count == 0)
            return null;

        return new VehicleCatalogDetails(rows[0].CatalogItem, rows.Where(row => row.Image is not null).Select(row => row.Image!).ToArray());
    }

    public Task<bool> IsAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime expectedReturnDateTime) =>
        executor.ExecuteScalarBooleanAsync("usp_Vehicles_IsAvailable", p =>
        {
            AddInt(p, "@VehicleId", vehicleId);
            AddDateTime(p, "@PickupDateTime", pickupDateTime);
            AddDateTime(p, "@ExpectedReturnDateTime", expectedReturnDateTime);
        });
    public Task SetStatusAsync(int vehicleId, VehicleStatus status) => Run(() => executor.ExecuteNonQueryAsync(
        "usp_Vehicles_SetStatus", p =>
        {
            AddInt(p, "@VehicleId", vehicleId);
            AddText(p, "@Status", 20, status.ToString());
        }));
    public Task LockForReservationReviewAsync(int vehicleId) => Run(() => executor.ExecuteNonQueryAsync(
        "usp_Vehicles_LockForReservationReview", p => AddInt(p, "@VehicleId", vehicleId)));
    public Task<bool> HasOtherConfirmedReservationAsync(int vehicleId, int excludingReservationId) =>
        executor.ExecuteScalarBooleanAsync("usp_Vehicles_HasOtherConfirmedReservation", p =>
        {
            AddInt(p, "@VehicleId", vehicleId);
            AddInt(p, "@ExcludingReservationId", excludingReservationId);
        });
    public Task UpdateAsync(Vehicle vehicle) => Run(() => executor.ExecuteNonQueryAsync("usp_Vehicles_Update",p=>{AddInt(p,"@VehicleId",vehicle.VehicleId);Configure(p,vehicle);}));
    public Task DeleteAsync(int id) => Run(() => executor.ExecuteNonQueryAsync("usp_Vehicles_Delete",p=>AddInt(p,"@VehicleId",id)));
    private static void Configure(SqlParameterCollection p,Vehicle v)
    {
        AddText(p,"@PlateNumber",20,v.PlateNumber);AddText(p,"@Make",100,v.Make);AddText(p,"@Model",100,v.Model);
        AddInt(p,"@Year",v.Year);AddInt(p,"@CategoryId",v.CategoryId);AddInt(p,"@FuelTypeId",v.FuelTypeId);
        AddInt(p,"@TransmissionTypeId",v.TransmissionTypeId);AddInt(p,"@SeatsCount",v.SeatsCount);
        var price=p.Add("@DailyPrice",SqlDbType.Decimal);price.Precision=18;price.Scale=2;price.Value=v.DailyPrice;AddInt(p,"@BranchId",v.BranchId);AddInt(p,"@CurrentMileage",v.CurrentMileage);
    }

    private static void ConfigureSearch(SqlParameterCollection p, VehicleSearchCriteria criteria)
    {
        AddNullableInt(p, "@CategoryId", criteria.CategoryId);
        AddNullableInt(p, "@FuelTypeId", criteria.FuelTypeId);
        AddNullableInt(p, "@TransmissionTypeId", criteria.TransmissionTypeId);
        AddNullableInt(p, "@BranchId", criteria.BranchId);
        AddNullableDecimal(p, "@MinimumDailyPrice", criteria.MinimumDailyPrice);
        AddNullableDecimal(p, "@MaximumDailyPrice", criteria.MaximumDailyPrice);
        AddNullableInt(p, "@MinimumSeats", criteria.MinimumSeats);
        AddNullableDateTime(p, "@PickupDateTime", criteria.PickupDateTimeUtc);
        AddNullableDateTime(p, "@ExpectedReturnDateTime", criteria.ExpectedReturnDateTimeUtc);
        AddInt(p, "@Page", criteria.Page);
        AddInt(p, "@PageSize", criteria.PageSize);
        p.Add("@SortBy", SqlDbType.NVarChar, 20).Value = criteria.SortBy;
        p.Add("@SortDirection", SqlDbType.NVarChar, 4).Value = criteria.SortDirection;
    }

    private static VehicleCatalogItem MapCatalogItem(SqlDataReader reader) => new(
        MapVehicle(reader),
        reader.GetString(reader.GetOrdinal("CategoryName")),
        reader.GetString(reader.GetOrdinal("FuelTypeName")),
        reader.GetString(reader.GetOrdinal("TransmissionTypeName")),
        reader.IsDBNull(reader.GetOrdinal("PrimaryImageUrl")) ? null : reader.GetString(reader.GetOrdinal("PrimaryImageUrl")));

    private static CatalogDetailsRow MapCatalogDetailsRow(SqlDataReader reader)
    {
        var item = MapCatalogItem(reader);
        var imageIdOrdinal = reader.GetOrdinal("ImageId");
        VehicleImage? image = reader.IsDBNull(imageIdOrdinal) ? null : new VehicleImage
        {
            ImageId = reader.GetInt32(imageIdOrdinal),
            VehicleId = reader.GetInt32(reader.GetOrdinal("ImageVehicleId")),
            ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
            IsPrimary = reader.GetBoolean(reader.GetOrdinal("ImageIsPrimary"))
        };
        return new CatalogDetailsRow(item, image);
    }

    private static Vehicle MapVehicle(SqlDataReader reader) => new()
    {
        VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
        PlateNumber = reader.GetString(reader.GetOrdinal("PlateNumber")),
        Make = reader.GetString(reader.GetOrdinal("Make")),
        Model = reader.GetString(reader.GetOrdinal("Model")),
        Year = reader.GetInt32(reader.GetOrdinal("Year")),
        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
        FuelTypeId = reader.GetInt32(reader.GetOrdinal("FuelTypeId")),
        TransmissionTypeId = reader.GetInt32(reader.GetOrdinal("TransmissionTypeId")),
        SeatsCount = reader.GetInt32(reader.GetOrdinal("SeatsCount")),
        DailyPrice = reader.GetDecimal(reader.GetOrdinal("DailyPrice")),
        BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
        Status = reader.GetString(reader.GetOrdinal("Status")),
        CurrentMileage = reader.GetInt32(reader.GetOrdinal("CurrentMileage"))
    };

    private static void AddNullableInt(SqlParameterCollection p, string name, int? value) =>
        p.Add(name, SqlDbType.Int).Value = value is null ? DBNull.Value : value.Value;
    private static void AddNullableDateTime(SqlParameterCollection p, string name, DateTime? value) =>
        p.Add(name, SqlDbType.DateTime2).Value = value is null ? DBNull.Value : value.Value;
    private static void AddDateTime(SqlParameterCollection p, string name, DateTime value) =>
        p.Add(name, SqlDbType.DateTime2).Value = value;
    private static void AddNullableDecimal(SqlParameterCollection p, string name, decimal? value)
    {
        var parameter = p.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value is null ? DBNull.Value : value.Value;
    }

    private sealed record CatalogDetailsRow(VehicleCatalogItem CatalogItem, VehicleImage? Image);
    private static async Task<T> Run<T>(Func<Task<T>> action){try{return await action();}catch(SqlException ex){SqlErrorTranslator.ThrowIfKnown(ex);throw;}}
    private static void AddInt(SqlParameterCollection p,string n,int v)=>p.Add(n,SqlDbType.Int).Value=v;
    private static void AddText(SqlParameterCollection p,string n,int size,string v)=>p.Add(n,SqlDbType.NVarChar,size).Value=v;
}
