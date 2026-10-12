CREATE OR ALTER PROCEDURE dbo.usp_Branches_Update
    @BranchId INT, @Name NVARCHAR(100), @Address NVARCHAR(255), @City NVARCHAR(100), @Phone NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Branches SET Name=@Name, Address=@Address, City=@City, Phone=@Phone WHERE BranchId=@BranchId;
    IF @@ROWCOUNT = 0 THROW 50040, 'Branch does not exist.', 1;
END;
GO
