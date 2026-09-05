-- =====================================================================
-- Table: Roles
-- Purpose: System roles used for RBAC (Admin, Employee, Customer, ...).
-- =====================================================================
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles
(
    RoleId      INT IDENTITY(1,1)  NOT NULL,
    RoleName    NVARCHAR(50)       NOT NULL,

    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO
