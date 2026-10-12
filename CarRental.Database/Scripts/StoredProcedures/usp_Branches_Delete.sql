CREATE OR ALTER PROCEDURE dbo.usp_Branches_Delete @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Employees WHERE BranchId=@BranchId) OR EXISTS (SELECT 1 FROM dbo.Vehicles WHERE BranchId=@BranchId)
        THROW 50041, 'Branch has dependent records.', 1;
    DELETE dbo.Branches WHERE BranchId=@BranchId;
    IF @@ROWCOUNT = 0 THROW 50040, 'Branch does not exist.', 1;
END;
GO
