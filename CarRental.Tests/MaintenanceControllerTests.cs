using _CarRental.API.Controllers;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace _CarRental.Tests;

public sealed class MaintenanceControllerTests
{
    [Fact]
    public async Task CreateReturnsCreatedAtMaintenanceRoute()
    {
        var service = new FakeMaintenanceService();
        var controller = new MaintenanceController(service);
        var result = await controller.Create(new CreateMaintenanceRequest(9, "Repair", null, new DateOnly(2026, 10, 19), null));
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(MaintenanceController.Get), created.ActionName);
        Assert.Equal(4, created.RouteValues!["maintenanceId"]);
        Assert.Equal(9, service.Request?.VehicleId);
    }

    [Fact]
    public void MaintenanceController_RestrictsAccessToStaff()
    {
        var authorize = typeof(MaintenanceController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal("Employee,BranchManager,Admin", authorize.Roles);
    }

    private sealed class FakeMaintenanceService : IMaintenanceService
    {
        public CreateMaintenanceRequest? Request { get; private set; }
        private static MaintenanceDto Record(int id = 4) => new(id, 9, 2, 5, "Repair", null,
            new DateOnly(2026, 10, 19), null, null, null, "InProgress");
        public Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request) { Request = request; return Task.FromResult(Record()); }
        public Task<MaintenanceDto> UpdateAsync(int maintenanceId, UpdateMaintenanceRequest request) => Task.FromResult(Record(maintenanceId));
        public Task<MaintenanceDto> CompleteAsync(int maintenanceId, CompleteMaintenanceRequest request) => Task.FromResult(Record(maintenanceId));
        public Task<MaintenanceDto> CancelAsync(int maintenanceId) => Task.FromResult(Record(maintenanceId));
        public Task<MaintenanceDto> GetAsync(int maintenanceId) => Task.FromResult(Record(maintenanceId));
        public Task<IReadOnlyList<MaintenanceDto>> ListByVehicleAsync(int vehicleId) => Task.FromResult<IReadOnlyList<MaintenanceDto>>([Record()]);
        public Task<_CarRental.Application.Common.Models.PagedResult<MaintenanceDto>> ListByBranchAsync(int branchId, _CarRental.Application.Common.Models.PagedRequest request) =>
            Task.FromResult(new _CarRental.Application.Common.Models.PagedResult<MaintenanceDto>([Record()], request.Page, request.PageSize, 1));
    }
}
