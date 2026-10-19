using _CarRental.Application.DTOs.Reviews;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class ReviewRequestValidator
{
    public void Validate(CreateReviewRequest request) => ValidateValues(request?.Rating, request?.Comment);
    public void Validate(UpdateReviewRequest request) => ValidateValues(request?.Rating, request?.Comment);

    private static void ValidateValues(int? rating, string? comment)
    {
        if (rating is null or < 1 or > 5)
            throw new BusinessRuleException("VALIDATION_FAILED", "Rating must be between 1 and 5.");
        if (comment?.Length > 1000)
            throw new BusinessRuleException("VALIDATION_FAILED", "Review comment cannot exceed 1000 characters.");
    }
}
