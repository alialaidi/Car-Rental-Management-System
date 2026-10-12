CREATE OR ALTER PROCEDURE dbo.usp_Customers_GetByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerId, UserId, FullName, DrivingLicenseNumber, DrivingLicenseExpiry, Address
    FROM dbo.Customers
    WHERE UserId = @UserId;
END;
GO
