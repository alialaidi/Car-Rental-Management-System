CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_SetStatus
    @VehicleId INT,
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status NOT IN (N'Available', N'Reserved')
        THROW 50002, 'Reservation workflow can only set Available or Reserved.', 1;

    UPDATE dbo.Vehicles
    SET Status = @Status
    WHERE VehicleId = @VehicleId
      AND Status IN (N'Available', N'Reserved');

    IF @@ROWCOUNT = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE VehicleId = @VehicleId)
            THROW 50044, 'Vehicle was not found.', 1;
        THROW 50004, 'Vehicle status cannot be changed by the reservation workflow.', 1;
    END
END;
GO
