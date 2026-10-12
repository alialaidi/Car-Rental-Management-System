CREATE OR ALTER PROCEDURE dbo.usp_UserRoles_Remove @UserId INT, @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE dbo.UserRoles WHERE UserId=@UserId AND RoleId=@RoleId;
END;
GO
