-- =====================================================================
-- Table: Employees
-- Purpose: Employee profile, 1-to-1 with Users, belongs to a Branch.
-- =====================================================================
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
GO

CREATE TABLE dbo.Employees
(
    EmployeeId  INT IDENTITY(1,1)  NOT NULL,
    UserId      INT                NOT NULL,
    BranchId    INT                NOT NULL,
    Position    NVARCHAR(100)      NOT NULL,
    HireDate    DATE               NOT NULL,

    CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED (EmployeeId),
    CONSTRAINT UQ_Employees_UserId UNIQUE (UserId),
    -- NOTE: no ON DELETE CASCADE here (Branches already has a FK back to
    -- Employees for ManagerEmployeeId -> would create multiple cascade paths).
    CONSTRAINT FK_Employees_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Employees_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (BranchId)
);
GO

CREATE NONCLUSTERED INDEX IX_Employees_BranchId ON dbo.Employees (BranchId);
GO
