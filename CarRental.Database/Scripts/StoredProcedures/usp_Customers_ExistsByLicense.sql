CREATE OR ALTER PROCEDURE dbo.usp_Customers_ExistsByLicense
    @DrivingLicenseNumber NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.Customers WHERE DrivingLicenseNumber = @DrivingLicenseNumber
    ) THEN 1 ELSE 0 END);
END;
GO
