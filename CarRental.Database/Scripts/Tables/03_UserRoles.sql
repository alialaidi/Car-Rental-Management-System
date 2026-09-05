-- =====================================================================
-- Table: UserRoles
-- Purpose: Many-to-many mapping between Users and Roles.
-- =====================================================================
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
GO

CREATE TABLE dbo.UserRoles
(
    UserId  INT NOT NULL,
    RoleId  INT NOT NULL,

    CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId) ON DELETE CASCADE
);
GO
