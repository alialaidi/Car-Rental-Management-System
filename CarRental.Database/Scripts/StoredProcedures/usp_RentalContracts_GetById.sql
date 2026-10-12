CREATE OR ALTER PROCEDURE dbo.usp_RentalContracts_GetById @ContractId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ContractId, c.ReservationId, c.ContractNumber, c.CustomerId, c.VehicleId, c.EmployeeId,
           c.ActualPickupDate, c.ActualReturnDate, c.PickupMileage, c.ReturnMileage,
           c.PickupFuelLevel, c.ReturnFuelLevel, c.ConditionNotes, c.RentalDays,
           c.BasicRentalPrice, c.DiscountAmount, c.AdditionalFees, c.FuelCharges, c.DamageCharges,
           c.TotalAmount, c.Status, r.PickupBranchId
    FROM dbo.RentalContracts c
    LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    WHERE c.ContractId = @ContractId;
END;
GO
