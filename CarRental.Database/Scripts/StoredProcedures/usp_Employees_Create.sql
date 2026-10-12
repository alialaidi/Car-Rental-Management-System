CREATE OR ALTER PROCEDURE dbo.usp_Employees_Create
    @UserId INT, @BranchId INT, @Position NVARCHAR(100), @HireDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Employees (UserId, BranchId, Position, HireDate) VALUES (@UserId, @BranchId, @Position, @HireDate);
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END;
GO
