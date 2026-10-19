using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reviews;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class ReviewService(IUnitOfWork unitOfWork, ICurrentUser currentUser,
    IAuditLogger auditLogger, ReviewRequestValidator validator) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(int contractId, CreateReviewRequest request)
    {
        validator.Validate(request);
        var customerId = await currentUser.GetCustomerIdAsync();
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        if (contract.CustomerId != customerId) throw new NotFoundException("Contract was not found.");
        if (contract.Status != ContractStatus.Completed)
            throw new ConflictException("CONTRACT_NOT_COMPLETED", "Only completed contracts can be reviewed.");
        if (await unitOfWork.Reviews.GetByContractAsync(contractId) is not null)
            throw new ConflictException("REVIEW_ALREADY_EXISTS", "A review already exists for this contract.");

        var review = Review.Create(contractId, request.Rating, request.Comment);
        await InTransactionAsync(async () => review.ReviewId = await unitOfWork.Reviews.CreateAsync(review, customerId));
        var saved = await unitOfWork.Reviews.GetByIdAsync(review.ReviewId)
            ?? throw new InvalidOperationException("The created review could not be reloaded.");
        await AuditAsync(AuditActions.ReviewCreated, saved.ReviewId, null, ToDto(saved));
        return ToDto(saved);
    }

    public async Task<ReviewDto> UpdateAsync(int reviewId, UpdateReviewRequest request)
    {
        validator.Validate(request);
        var customerId = await currentUser.GetCustomerIdAsync();
        var review = await RequireReviewAsync(reviewId);
        EnsureOwner(review, customerId);
        var old = ToDto(review);
        review.Update(request.Rating, request.Comment);
        await InTransactionAsync(() => unitOfWork.Reviews.UpdateAsync(review, customerId));
        var updated = await RequireReviewAsync(reviewId);
        await AuditAsync(AuditActions.ReviewUpdated, reviewId, old, ToDto(updated));
        return ToDto(updated);
    }

    public async Task DeleteAsync(int reviewId)
    {
        var customerId = await currentUser.GetCustomerIdAsync();
        var review = await RequireReviewAsync(reviewId);
        EnsureOwner(review, customerId);
        await InTransactionAsync(() => unitOfWork.Reviews.DeleteAsync(reviewId, customerId));
        await AuditAsync(AuditActions.ReviewDeleted, reviewId, ToDto(review), new { Result = "deleted" });
    }

    public async Task<VehicleReviewsDto> ListByVehicleAsync(int vehicleId, PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (vehicleId <= 0 || await unitOfWork.Vehicles.GetByIdAsync(vehicleId) is null)
            throw new NotFoundException("Vehicle was not found.");
        var page = await unitOfWork.Reviews.ListByVehicleAsync(vehicleId, request.Page, request.PageSize);
        return new VehicleReviewsDto(page.Items.Select(ToDto).ToArray(), page.AverageRating,
            page.ReviewCount, request.Page, request.PageSize, page.TotalCount);
    }

    private async Task<Review> RequireReviewAsync(int id)
    {
        if (id <= 0) throw new NotFoundException("Review was not found.");
        return await unitOfWork.Reviews.GetByIdAsync(id) ?? throw new NotFoundException("Review was not found.");
    }

    private static void EnsureOwner(Review review, int customerId)
    {
        if (review.CustomerId != customerId) throw new NotFoundException("Review was not found.");
    }

    private async Task InTransactionAsync(Func<Task> operation)
    {
        await unitOfWork.BeginTransactionAsync();
        try { await operation(); await unitOfWork.CommitAsync(); }
        catch { await unitOfWork.RollbackAsync(); throw; }
    }

    private Task AuditAsync(string action, int reviewId, object? oldValue, object? newValue) =>
        auditLogger.LogAsync(action, "Reviews", reviewId.ToString(CultureInfo.InvariantCulture), oldValue, newValue, currentUser.UserId);

    private static ReviewDto ToDto(Review review) => new(review.ReviewId, review.ContractId,
        review.CustomerId, review.CustomerName, review.VehicleId, review.Rating, review.Comment,
        DateTime.SpecifyKind(review.CreatedAt, DateTimeKind.Utc));
}
