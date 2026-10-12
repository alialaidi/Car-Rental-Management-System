using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Policy = Policies.ManageEmployees)]
public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request)
    {
        var employee = await employeeService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { employeeId = employee.EmployeeId }, employee);
    }

    [HttpGet("{employeeId:int}")]
    public Task<EmployeeDto> Get(int employeeId) => employeeService.GetAsync(employeeId);

    [HttpGet]
    public Task<PagedResult<EmployeeDto>> List([FromQuery] int branchId, [FromQuery] PagedRequest request) =>
        employeeService.ListByBranchAsync(branchId, request);

    [HttpPut("{employeeId:int}")]
    public Task<EmployeeDto> Update(int employeeId, UpdateEmployeeRequest request) => employeeService.UpdateAsync(employeeId, request);
}
