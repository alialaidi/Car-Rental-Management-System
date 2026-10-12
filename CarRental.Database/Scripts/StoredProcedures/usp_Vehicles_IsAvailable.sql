CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_IsAvailable
    @VehicleId INT,
    @PickupDateTime DATETIME2(3),
    @ExpectedReturnDateTime DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IsAvailable
    FROM dbo.fn_Vehicles_IsAvailable(@VehicleId, @PickupDateTime, @ExpectedReturnDateTime);
END;
GO
