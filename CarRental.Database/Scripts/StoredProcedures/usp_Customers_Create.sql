CREATE OR ALTER PROCEDURE dbo.usp_Customers_Create
    @UserId INT,
    @FullName NVARCHAR(150),
    @DrivingLicenseNumber NVARCHAR(50),
    @DrivingLicenseExpiry DATE,
    @Address NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Customers WHERE DrivingLicenseNumber = @DrivingLicenseNumber)
        THROW 50022, 'Driving license number already exists.', 1;

    INSERT INTO dbo.Customers (UserId, FullName, DrivingLicenseNumber, DrivingLicenseExpiry, Address)
    VALUES (@UserId, @FullName, @DrivingLicenseNumber, @DrivingLicenseExpiry, @Address);

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS CustomerId;
END;
GO
