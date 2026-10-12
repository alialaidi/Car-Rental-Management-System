CREATE OR ALTER PROCEDURE dbo.usp_Employees_Update
    @EmployeeId INT, @BranchId INT, @Position NVARCHAR(100), @HireDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Employees SET BranchId=@BranchId, Position=@Position, HireDate=@HireDate WHERE EmployeeId=@EmployeeId;
    IF @@ROWCOUNT = 0 THROW 50043, 'Employee does not exist.', 1;
END;
GO
