CREATE OR ALTER PROCEDURE dbo.usp_Roles_GetIdByName
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RoleId FROM dbo.Roles WHERE RoleName = @RoleName;
END;
GO
