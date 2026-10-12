CREATE OR ALTER PROCEDURE dbo.usp_Users_Exists
    @Username NVARCHAR(50) = NULL,
    @Email NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE (@Username IS NOT NULL AND Username = @Username)
           OR (@Email IS NOT NULL AND Email = @Email)
    ) THEN 1 ELSE 0 END);
END;
GO
