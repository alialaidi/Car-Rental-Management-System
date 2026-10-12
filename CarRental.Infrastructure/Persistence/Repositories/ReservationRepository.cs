using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class ReservationRepository(SqlExecutor executor) : IReservationRepository
{
    public Task<int> CreateAsync(Reservation reservation) => Run(() => executor.ExecuteScalarInt32Async(
        "usp_Reservations_Create", parameters =>
        {
            AddInt(parameters, "@CustomerId", reservation.CustomerId);
            AddInt(parameters, "@VehicleId", reservation.VehicleId);
            AddInt(parameters, "@PickupBranchId", reservation.PickupBranchId);
            AddInt(parameters, "@ReturnBranchId", reservation.ReturnBranchId);
            AddDateTime(parameters, "@PickupDateTime", reservation.PickupDateTime);
            AddDateTime(parameters, "@ExpectedReturnDateTime", reservation.ExpectedReturnDateTime);
        }));

    public Task<Reservation?> GetByIdAsync(int reservationId) => executor.QuerySingleOrDefaultAsync(
        "usp_Reservations_GetById", ReservationMapper.Map,
        parameters => AddInt(parameters, "@ReservationId", reservationId));

    public async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize)
    {
        var (counts, items) = await executor.QueryTwoResultSetsAsync(
            "usp_Reservations_ListByCustomer",
            reader => reader.GetInt32(reader.GetOrdinal("TotalCount")),
            ReservationMapper.Map,
            parameters =>
            {
                AddInt(parameters, "@CustomerId", customerId);
                AddInt(parameters, "@Page", page);
                AddInt(parameters, "@PageSize", pageSize);
            });
        return (items, counts.FirstOrDefault());
    }

    public async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListForBranchAsync(
        int? branchId, ReservationStatus? status, DateTime? pickupFromUtc, DateTime? pickupToUtc, int page, int pageSize)
    {
        var (counts, items) = await executor.QueryTwoResultSetsAsync(
            "usp_Reservations_ListForBranch",
            reader => reader.GetInt32(reader.GetOrdinal("TotalCount")),
            ReservationMapper.Map,
            parameters =>
            {
                AddNullableInt(parameters, "@BranchId", branchId);
                parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status is null ? DBNull.Value : status.Value.ToString();
                AddNullableDateTime(parameters, "@PickupFrom", pickupFromUtc);
                AddNullableDateTime(parameters, "@PickupTo", pickupToUtc);
                AddInt(parameters, "@Page", page);
                AddInt(parameters, "@PageSize", pageSize);
            });
        return (items, counts.FirstOrDefault());
    }

    public Task UpdateStatusAsync(int reservationId, ReservationStatus expectedStatus, ReservationStatus newStatus, int? confirmedByEmployeeId = null) => Run(() =>
        executor.ExecuteNonQueryAsync("usp_Reservations_UpdateStatus", parameters =>
        {
            AddInt(parameters, "@ReservationId", reservationId);
            parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = expectedStatus.ToString();
            parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = newStatus.ToString();
            parameters.Add("@ConfirmedByEmployeeId", SqlDbType.Int).Value = (object?)confirmedByEmployeeId ?? DBNull.Value;
        }));

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }

    private static void AddInt(SqlParameterCollection parameters, string name, int value) =>
        parameters.Add(name, SqlDbType.Int).Value = value;

    private static void AddDateTime(SqlParameterCollection parameters, string name, DateTime value) =>
        parameters.Add(name, SqlDbType.DateTime2).Value = value;

    private static void AddNullableDateTime(SqlParameterCollection parameters, string name, DateTime? value) =>
        parameters.Add(name, SqlDbType.DateTime2).Value = value is null ? DBNull.Value : value.Value;

    private static void AddNullableInt(SqlParameterCollection parameters, string name, int? value) =>
        parameters.Add(name, SqlDbType.Int).Value = value is null ? DBNull.Value : value.Value;


}
