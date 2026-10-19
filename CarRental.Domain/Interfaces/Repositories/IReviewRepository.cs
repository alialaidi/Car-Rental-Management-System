using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IReviewRepository
{
    Task<int> CreateAsync(Review review, int customerId);
    Task UpdateAsync(Review review, int customerId);
    Task DeleteAsync(int reviewId, int customerId);
    Task<Review?> GetByIdAsync(int reviewId);
    Task<Review?> GetByContractAsync(int contractId);
    Task<ReviewPage> ListByVehicleAsync(int vehicleId, int page, int pageSize);
}

public sealed record ReviewPage(IReadOnlyList<Review> Items, int TotalCount, int ReviewCount, decimal? AverageRating);
