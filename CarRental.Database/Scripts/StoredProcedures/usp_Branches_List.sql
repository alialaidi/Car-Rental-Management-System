CREATE OR ALTER PROCEDURE dbo.usp_Branches_List @Page INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BranchId, Name, Address, City, Phone, ManagerEmployeeId
    FROM dbo.Branches ORDER BY BranchId OFFSET (CONVERT(BIGINT, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

