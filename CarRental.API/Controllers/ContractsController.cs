using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController(IRentalContractService contractService) : ControllerBase
{


    [HttpPost]
    [Authorize(Roles = "Employee,BranchManager,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Create(CreateContractRequest request)
    {
        var contract = await contractService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { contractId = contract.ContractId }, contract);
    }




    [HttpGet("{contractId:int}")]
    [Authorize(Roles = "Customer,Employee,BranchManager,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ContractDto> GetById(int contractId) => contractService.GetByIdAsync(contractId);



    [HttpPost("{contractId:int}/return")]
    [Authorize(Roles = "Employee,BranchManager,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ContractDto> Return(int contractId, ReturnVehicleRequest request) =>
        contractService.ReturnAsync(contractId, request);



    [HttpGet]
    [Authorize(Roles = "Employee,BranchManager,Admin")]
    [ProducesResponseType(typeof(PagedResult<ContractDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<ContractDto>> List([FromQuery] PagedRequest request) => contractService.ListAsync(request);



    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(PagedResult<ContractDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<ContractDto>> ListMine([FromQuery] PagedRequest request) => contractService.ListMineAsync(request);


}
