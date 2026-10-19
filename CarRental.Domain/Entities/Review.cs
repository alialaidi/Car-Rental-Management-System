using _CarRental.Domain.Exceptions;

namespace _CarRental.Domain.Entities;

public sealed class Review
{
    public int ReviewId { get; set; }
    public int ContractId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Review Create(int contractId, int rating, string? comment)
    {
        Validate(contractId, rating, comment);
        return new Review { ContractId = contractId, Rating = rating, Comment = Normalize(comment) };
    }

    public void Update(int rating, string? comment)
    {
        Validate(ContractId, rating, comment);
        Rating = rating;
        Comment = Normalize(comment);
    }

    public static Review FromPersistence(int reviewId, int contractId, int customerId, int vehicleId,
        string customerName, int rating, string? comment, DateTime createdAt) => new()
    {
        ReviewId = reviewId, ContractId = contractId, CustomerId = customerId, VehicleId = vehicleId,
        CustomerName = customerName, Rating = rating, Comment = comment, CreatedAt = createdAt
    };

    private static void Validate(int contractId, int rating, string? comment)
    {
        if (contractId <= 0) throw new BusinessRuleException("VALIDATION_FAILED", "Contract id must be positive.");
        if (rating is < 1 or > 5) throw new BusinessRuleException("VALIDATION_FAILED", "Rating must be between 1 and 5.");
        if (comment?.Length > 1000) throw new BusinessRuleException("VALIDATION_FAILED", "Review comment cannot exceed 1000 characters.");
    }

    private static string? Normalize(string? comment) => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
}
