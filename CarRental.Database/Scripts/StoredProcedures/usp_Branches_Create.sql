CREATE OR ALTER PROCEDURE dbo.usp_Branches_Create
    @Name NVARCHAR(100), @Address NVARCHAR(255), @City NVARCHAR(100), @Phone NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Branches (Name, Address, City, Phone) VALUES (@Name, @Address, @City, @Phone);
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END;
GO
