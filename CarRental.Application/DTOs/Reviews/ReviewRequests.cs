namespace _CarRental.Application.DTOs.Reviews;

public sealed record CreateReviewRequest(int Rating, string? Comment = null);
public sealed record UpdateReviewRequest(int Rating, string? Comment = null);
public sealed record ReviewDto(int ReviewId, int ContractId, int CustomerId, string CustomerName,
    int VehicleId, int Rating, string? Comment, DateTime CreatedAtUtc);
public sealed record VehicleReviewsDto(IReadOnlyList<ReviewDto> Items, decimal? AverageRating,
    int ReviewCount, int Page, int PageSize, int TotalCount);
