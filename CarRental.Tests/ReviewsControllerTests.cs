using _CarRental.API.Controllers;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReviewsControllerTests
{
    [Fact]
    public async Task CreateReturnsCreatedAtVehicleReviewList()
    {
        var service = new FakeReviewService();
        var controller = new ReviewsController(service);
        var result = await controller.Create(8, new CreateReviewRequest(5, "Good"));
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ReviewsController.ListByVehicle), created.ActionName);
        Assert.Equal(3, created.RouteValues!["vehicleId"]);
        Assert.Equal(8, service.ContractId);
    }

    [Fact]
    public void ReviewWritesAreCustomerOnlyAndVehicleReviewsAreReadableByUsers()
    {
        Assert.Equal("Customer", Roles(nameof(ReviewsController.Create)));
        Assert.Equal("Customer", Roles(nameof(ReviewsController.Update)));
        Assert.Equal("Customer", Roles(nameof(ReviewsController.Delete)));
        Assert.Equal("Customer,Employee,BranchManager,Admin", Roles(nameof(ReviewsController.ListByVehicle)));
    }

    private static string? Roles(string name) => typeof(ReviewsController).GetMethod(name)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles;

    private sealed class FakeReviewService : IReviewService
    {
        public int ContractId { get; private set; }
        private static ReviewDto Sample(int vehicleId = 3) => new(2, 8, 9, "Customer", vehicleId, 5, "Good", DateTime.UtcNow);
        public Task<ReviewDto> CreateAsync(int contractId, CreateReviewRequest request) { ContractId = contractId; return Task.FromResult(Sample()); }
        public Task<ReviewDto> UpdateAsync(int reviewId, UpdateReviewRequest request) => Task.FromResult(Sample());
        public Task DeleteAsync(int reviewId) => Task.CompletedTask;
        public Task<VehicleReviewsDto> ListByVehicleAsync(int vehicleId, PagedRequest request) =>
            Task.FromResult(new VehicleReviewsDto([Sample(vehicleId)], 5m, 1, request.Page, request.PageSize, 1));
    }
}
