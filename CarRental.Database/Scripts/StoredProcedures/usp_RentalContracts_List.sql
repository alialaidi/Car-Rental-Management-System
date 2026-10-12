CREATE OR ALTER PROCEDURE dbo.usp_RentalContracts_List @BranchId INT = NULL, @Page INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS TotalCount
    FROM dbo.RentalContracts c LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    WHERE @BranchId IS NULL OR r.PickupBranchId = @BranchId;

    SELECT c.ContractId, c.ReservationId, c.ContractNumber, c.CustomerId, c.VehicleId, c.EmployeeId,
           c.ActualPickupDate, c.ActualReturnDate, c.PickupMileage, c.ReturnMileage,
           c.PickupFuelLevel, c.ReturnFuelLevel, c.ConditionNotes, c.RentalDays,
           c.BasicRentalPrice, c.DiscountAmount, c.AdditionalFees, c.FuelCharges, c.DamageCharges,
           c.TotalAmount, c.Status, r.PickupBranchId
    FROM dbo.RentalContracts c LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    WHERE @BranchId IS NULL OR r.PickupBranchId = @BranchId
    ORDER BY c.ActualPickupDate DESC, c.ContractId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
