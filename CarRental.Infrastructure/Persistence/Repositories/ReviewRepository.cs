using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class ReviewRepository(SqlExecutor executor) : IReviewRepository
{
    public Task<int> CreateAsync(Review review, int customerId) => Run(() => executor.ExecuteScalarInt32Async("usp_Reviews_Create", p =>
    {
        AddInt(p, "@ContractId", review.ContractId); AddInt(p, "@CustomerId", customerId);
        AddInt(p, "@Rating", review.Rating); AddNullableText(p, "@Comment", 1000, review.Comment);
    }));

    public Task UpdateAsync(Review review, int customerId) => Run(() => executor.ExecuteNonQueryAsync("usp_Reviews_Update", p =>
    {
        AddInt(p, "@ReviewId", review.ReviewId); AddInt(p, "@CustomerId", customerId);
        AddInt(p, "@Rating", review.Rating); AddNullableText(p, "@Comment", 1000, review.Comment);
    }));

    public Task DeleteAsync(int reviewId, int customerId) => Run(() => executor.ExecuteNonQueryAsync("usp_Reviews_Delete", p =>
    { AddInt(p, "@ReviewId", reviewId); AddInt(p, "@CustomerId", customerId); }));

    public Task<Review?> GetByIdAsync(int reviewId) => Run(() => executor.QuerySingleOrDefaultAsync("usp_Reviews_GetById", ReviewMapper.Map, p => AddInt(p, "@ReviewId", reviewId)));
    public Task<Review?> GetByContractAsync(int contractId) => Run(() => executor.QuerySingleOrDefaultAsync("usp_Reviews_GetByContract", ReviewMapper.Map, p => AddInt(p, "@ContractId", contractId)));

    public async Task<ReviewPage> ListByVehicleAsync(int vehicleId, int page, int pageSize)
    {
        var (summary, items) = await Run(() => executor.QueryTwoResultSetsAsync("usp_Reviews_ListByVehicle",
            r => new ReviewSummary(r.GetInt32(r.GetOrdinal("TotalCount")), r.GetInt32(r.GetOrdinal("ReviewCount")),
                r.IsDBNull(r.GetOrdinal("AverageRating")) ? null : r.GetDecimal(r.GetOrdinal("AverageRating"))),
            ReviewMapper.Map,
            p => { AddInt(p, "@VehicleId", vehicleId); AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); }));
        var result = summary.FirstOrDefault() ?? new ReviewSummary(0, 0, null);
        return new ReviewPage(items, result.TotalCount, result.ReviewCount, result.AverageRating);
    }

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }
    private static void AddInt(SqlParameterCollection p, string n, int v) => p.Add(n, SqlDbType.Int).Value = v;
    private static void AddNullableText(SqlParameterCollection p, string n, int size, string? value) => p.Add(n, SqlDbType.NVarChar, size).Value = (object?)value ?? DBNull.Value;
    private sealed record ReviewSummary(int TotalCount, int ReviewCount, decimal? AverageRating);
}
