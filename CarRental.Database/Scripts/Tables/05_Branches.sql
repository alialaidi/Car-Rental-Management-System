-- =====================================================================
-- Table: Branches
-- Purpose: Company branches/locations.
-- Note: ManagerEmployeeId FK is added later (06_Employees_AddManagerFK.sql)
--       to break the circular dependency Branches <-> Employees.
-- =====================================================================
IF OBJECT_ID('dbo.Branches', 'U') IS NOT NULL DROP TABLE dbo.Branches;
GO

CREATE TABLE dbo.Branches
(
    BranchId            INT IDENTITY(1,1)  NOT NULL,
    Name                NVARCHAR(100)      NOT NULL,
    Address             NVARCHAR(255)      NOT NULL,
    City                NVARCHAR(100)      NOT NULL,
    Phone               NVARCHAR(20)       NULL,
    ManagerEmployeeId   INT                NULL,   -- FK added after Employees table exists

    CONSTRAINT PK_Branches PRIMARY KEY CLUSTERED (BranchId)
);
GO
