using _CarRental.Application.DTOs.Maintenance;
using _CarRental.Application.Validators;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class MaintenanceTests
{
    private static readonly DateOnly Start = new(2026, 10, 19);

    [Fact]
    public void Open_RequiresValidDetailsAndExpectedEndNotBeforeStart()
    {
        Assert.Throws<BusinessRuleException>(() => MaintenanceRecord.Open(1, 1, 1, " ", null, Start, null));
        Assert.Throws<BusinessRuleException>(() => MaintenanceRecord.Open(1, 1, 1, "Oil service", null, Start, Start.AddDays(-1)));
    }

    [Fact]
    public void Completion_SetsEndCostAndTerminalStatus()
    {
        var record = OpenRecord();
        record.Complete(Start.AddDays(1), 45.50m);
        Assert.Equal(Start.AddDays(1), record.ActualEndDate);
        Assert.Equal(45.50m, record.Cost);
        Assert.Equal(MaintenanceStatus.Completed, record.Status);
        Assert.Throws<ConflictException>(() => record.Cancel());
    }

    [Fact]
    public void Cancellation_IsTerminal()
    {
        var record = OpenRecord();
        record.Cancel();
        Assert.Equal(MaintenanceStatus.Cancelled, record.Status);
        Assert.Throws<ConflictException>(() => record.UpdateDetails("Repair", null, null));
    }

    [Fact]
    public void Update_RejectsExcessiveTextAndNegativeCost()
    {
        var validator = new MaintenanceRequestValidator();
        Assert.Throws<BusinessRuleException>(() => validator.Validate(
            new UpdateMaintenanceRequest("x", new string('x', 1001), null), Start));
        Assert.Throws<BusinessRuleException>(() => validator.Validate(new CompleteMaintenanceRequest(-1)));
    }

    private static MaintenanceRecord OpenRecord() => MaintenanceRecord.Open(2, 3, 4, "Oil service", null, Start, Start.AddDays(2));
}
