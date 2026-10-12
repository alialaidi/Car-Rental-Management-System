using _CarRental.API.Controllers;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Contracts;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace _CarRental.Tests;

public sealed class ContractsControllerTests
{
    [Fact]
    public async Task CreateReturnsCreatedAtContractRoute()
    {
        var service = new FakeContractService();
        var controller = new ContractsController(service);

        var result = await controller.Create(new CreateContractRequest(77, null, FuelLevel.Full, null));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ContractsController.GetById), created.ActionName);
        Assert.Equal(1, created.RouteValues!["contractId"]);
        Assert.Same(service.Contract, created.Value);
    }

    [Fact]
    public async Task CustomerMyRouteDelegatesToCustomerContractList()
    {
        var service = new FakeContractService();
        var controller = new ContractsController(service);
        var request = new PagedRequest { Page = 2, PageSize = 5 };

        var result = await controller.ListMine(request);

        Assert.Equal(2, result.Page);
        Assert.Same(request, service.LastPagedRequest);
        Assert.Equal(nameof(FakeContractService.ListMineAsync), service.LastMethod);
    }

    [Fact]
    public void ContractRoutesHaveTheExpectedRoleRestrictions()
    {
        Assert.Equal("Employee,BranchManager,Admin", Roles(nameof(ContractsController.Create)));
        Assert.Equal("Employee,BranchManager,Admin", Roles(nameof(ContractsController.List)));
        Assert.Equal("Customer", Roles(nameof(ContractsController.ListMine)));
        Assert.Equal("Customer,Employee,BranchManager,Admin", Roles(nameof(ContractsController.GetById)));
        Assert.Equal("Employee,BranchManager,Admin", Roles(nameof(ContractsController.Return)));
    }

    private static string? Roles(string methodName) => typeof(ContractsController)
        .GetMethod(methodName)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
        .Cast<AuthorizeAttribute>()
        .Single()
        .Roles;

    private sealed class FakeContractService : IRentalContractService
    {
        public ContractDto Contract { get; } = new(1, 77, "RC-20261011-000001", 42, 3, 321,
            new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 100, "Full", null, 2,
            40m, 0m, 0m, 0m, 0m, 40m, "Active", 1);
        public PagedRequest? LastPagedRequest { get; private set; }
        public string? LastMethod { get; private set; }
        public Task<ContractDto> CreateAsync(CreateContractRequest request) => Task.FromResult(Contract);
        public Task<ContractDto> GetByIdAsync(int contractId) => Task.FromResult(Contract);
        public Task<ContractDto> ReturnAsync(int contractId, ReturnVehicleRequest request) => Task.FromResult(Contract);
        public Task<PagedResult<ContractDto>> ListAsync(PagedRequest request) => Page(nameof(ListAsync), request);
        public Task<PagedResult<ContractDto>> ListMineAsync(PagedRequest request) => Page(nameof(ListMineAsync), request);

        private Task<PagedResult<ContractDto>> Page(string method, PagedRequest request)
        {
            LastMethod = method;
            LastPagedRequest = request;
            return Task.FromResult(new PagedResult<ContractDto>([Contract], request.Page, request.PageSize, 1));
        }
    }
}
