CREATE OR ALTER PROCEDURE dbo.usp_UserRoles_GetNames
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.RoleName
    FROM dbo.UserRoles AS ur
    INNER JOIN dbo.Roles AS r ON r.RoleId = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY r.RoleName;
END;
GO
