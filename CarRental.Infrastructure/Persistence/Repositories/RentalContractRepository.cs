using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class RentalContractRepository(SqlExecutor executor) : IRentalContractRepository
{
    public Task<int> CreateFromReservationAsync(RentalContract contract) => Run(() => executor.ExecuteScalarInt32Async(
        "usp_RentalContracts_CreateFromReservation", p =>
        {
            AddInt(p, "@ReservationId", contract.ReservationId!.Value);
            AddInt(p, "@EmployeeId", contract.EmployeeId);
            AddDateTime(p, "@ActualPickupDate", contract.ActualPickupDate);
            AddInt(p, "@PickupMileage", contract.PickupMileage);
            AddText(p, "@PickupFuelLevel", 20, contract.PickupFuelLevel.ToString());
            AddNullableText(p, "@ConditionNotes", 1000, contract.ConditionNotes);
            AddInt(p, "@RentalDays", contract.RentalDays);
            AddDecimal(p, "@BasicRentalPrice", contract.BasicRentalPrice);
            AddDecimal(p, "@DiscountAmount", contract.DiscountAmount);
            AddDecimal(p, "@AdditionalFees", contract.AdditionalFees);
            AddDecimal(p, "@TotalAmount", contract.TotalAmount);
        }));

    public Task CompleteAsync(RentalContract contract, int returnBranchId, bool requiresMaintenance) => Run(() => executor.ExecuteNonQueryAsync(
        "usp_RentalContracts_Return", p =>
        {
            AddInt(p, "@ContractId", contract.ContractId);
            AddInt(p, "@ReturnedByEmployeeId", contract.ReturnedByEmployeeId!.Value);
            AddDateTime(p, "@ActualReturnDate", contract.ActualReturnDate!.Value);
            AddInt(p, "@ReturnMileage", contract.ReturnMileage!.Value);
            AddText(p, "@ReturnFuelLevel", 20, contract.ReturnFuelLevel!.Value.ToString());
            AddNullableText(p, "@ConditionNotes", 1000, contract.ConditionNotes);
            AddInt(p, "@RentalDays", contract.RentalDays);
            AddDecimal(p, "@BasicRentalPrice", contract.BasicRentalPrice);
            AddDecimal(p, "@FuelCharges", contract.FuelCharges);
            AddDecimal(p, "@DamageCharges", contract.DamageCharges);
            AddDecimal(p, "@TotalAmount", contract.TotalAmount);
            AddInt(p, "@ReturnBranchId", returnBranchId);
            AddText(p, "@VehicleStatus", 20, requiresMaintenance ? "Maintenance" : "Available");
        }));

    public Task<RentalContract?> GetByIdAsync(int contractId) => Run(() => executor.QuerySingleOrDefaultAsync(
        "usp_RentalContracts_GetById", RentalContractMapper.Map, p => AddInt(p, "@ContractId", contractId)));

    public async Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListAsync(int? branchId, int page, int pageSize)
    {
        var (counts, items) = await Run(() => executor.QueryTwoResultSetsAsync(
            "usp_RentalContracts_List", r => r.GetInt32(r.GetOrdinal("TotalCount")), RentalContractMapper.Map,
            p => { AddNullableInt(p, "@BranchId", branchId); AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); }));
        return (items, counts.FirstOrDefault());
    }

    public async Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize)
    {
        var (counts, items) = await Run(() => executor.QueryTwoResultSetsAsync(
            "usp_RentalContracts_ListByCustomer", r => r.GetInt32(r.GetOrdinal("TotalCount")), RentalContractMapper.Map,
            p => { AddInt(p, "@CustomerId", customerId); AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); }));
        return (items, counts.FirstOrDefault());
    }

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }

    private static void AddInt(SqlParameterCollection p, string name, int value) => p.Add(name, SqlDbType.Int).Value = value;
    
    private static void AddNullableInt(SqlParameterCollection p, string name, int? value) => p.Add(name, SqlDbType.Int).Value = (object?)value ?? DBNull.Value;
    
    private static void AddText(SqlParameterCollection p, string name, int size, string value) => p.Add(name, SqlDbType.NVarChar, size).Value = value;
   
    private static void AddNullableText(SqlParameterCollection p, string name, int size, string? value) => p.Add(name, SqlDbType.NVarChar, size).Value = (object?)value ?? DBNull.Value;
    
    private static void AddDateTime(SqlParameterCollection p, string name, DateTime value) => p.Add(name, SqlDbType.DateTime2).Value = value;
    
    private static void AddDecimal(SqlParameterCollection p, string name, decimal value)
    {
        var parameter = p.Add(name, SqlDbType.Decimal); parameter.Precision = 18; parameter.Scale = 2; parameter.Value = value;
    }


}
