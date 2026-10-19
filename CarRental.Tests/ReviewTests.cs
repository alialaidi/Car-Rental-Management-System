using _CarRental.Application.DTOs.Reviews;
using _CarRental.Application.Validators;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CreateRejectsRatingOutsideRange(int rating)
    {
        Assert.Throws<BusinessRuleException>(() => Review.Create(12, rating, null));
    }

    [Fact]
    public void CreateAndUpdateTrimComment()
    {
        var review = Review.Create(12, 4, "  Nice vehicle  ");
        Assert.Equal("Nice vehicle", review.Comment);
        review.Update(5, "  Great service ");
        Assert.Equal(5, review.Rating);
        Assert.Equal("Great service", review.Comment);
    }

    [Fact]
    public void ValidatorRejectsLongComment()
    {
        var validator = new ReviewRequestValidator();
        Assert.Throws<BusinessRuleException>(() => validator.Validate(new CreateReviewRequest(5, new string('a', 1001))));
    }
}
