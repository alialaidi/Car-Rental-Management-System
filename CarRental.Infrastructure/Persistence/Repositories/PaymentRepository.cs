using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(SqlExecutor executor) : IPaymentRepository
{
    public Task<int> CreateAsync(Payment payment) => Run(() => executor.ExecuteScalarInt32Async("usp_Payments_Create", p =>
    {
        AddInt(p, "@ContractId", payment.ContractId); AddText(p, "@PaymentMethod", 20, payment.Method.ToString());
        AddDecimal(p, "@Amount", payment.Amount); AddNullableText(p, "@TransactionReference", 100, payment.TransactionReference);
    }));

    public Task<Payment?> GetByIdAsync(int paymentId) => Run(() => executor.QuerySingleOrDefaultAsync("usp_Payments_GetById", PaymentMapper.Map, p => AddInt(p, "@PaymentId", paymentId)));

    public Task UpdateStatusAsync(int paymentId, string status) => Run(async () =>
    {
        await executor.ExecuteNonQueryAsync("usp_Payments_UpdateStatus", p => { AddInt(p, "@PaymentId", paymentId); AddText(p, "@PaymentStatus", 20, status); });
        return 0;
    });

    public Task<IReadOnlyList<Payment>> ListByContractAsync(int contractId) => Run(() => executor.QueryAsync("usp_Payments_ListByContract", PaymentMapper.Map, p => AddInt(p, "@ContractId", contractId)));
    
    public Task<IReadOnlyList<Payment>> ListByCustomerAsync(int customerId) => Run(() => executor.QueryAsync("usp_Payments_ListByCustomer", PaymentMapper.Map, p => AddInt(p, "@CustomerId", customerId)));

    public async Task<PaymentBalance> GetBalanceAsync(int contractId) => await Run(() => executor.QuerySingleOrDefaultAsync("usp_Payments_GetBalance", r => new PaymentBalance(
        r.GetInt32(r.GetOrdinal("ContractId")), r.GetDecimal(r.GetOrdinal("TotalAmount")), r.GetDecimal(r.GetOrdinal("PaidAmount")), r.GetDecimal(r.GetOrdinal("RefundedAmount"))), p => AddInt(p, "@ContractId", contractId)))
        ?? throw new _CarRental.Domain.Exceptions.NotFoundException("Contract was not found.");

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }

    private static void AddInt(SqlParameterCollection p, string n, int v) => p.Add(n, SqlDbType.Int).Value = v;
    
    private static void AddText(SqlParameterCollection p, string n, int s, string v) => p.Add(n, SqlDbType.NVarChar, s).Value = v;
    
    private static void AddNullableText(SqlParameterCollection p, string n, int s, string? v) => p.Add(n, SqlDbType.NVarChar, s).Value = (object?)v ?? DBNull.Value;
   
    private static void AddDecimal(SqlParameterCollection p, string n, decimal v) { var x = p.Add(n, SqlDbType.Decimal); x.Precision = 18; x.Scale = 2; x.Value = v; }

}
