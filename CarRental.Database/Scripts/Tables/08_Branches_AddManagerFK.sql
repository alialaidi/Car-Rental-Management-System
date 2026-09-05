-- =====================================================================
-- Adds the deferred FK: Branches.ManagerEmployeeId -> Employees.EmployeeId
-- Must run AFTER 05_Branches.sql and 07_Employees.sql.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Branches_ManagerEmployee')
BEGIN
    ALTER TABLE dbo.Branches
        ADD CONSTRAINT FK_Branches_ManagerEmployee
        FOREIGN KEY (ManagerEmployeeId) REFERENCES dbo.Employees (EmployeeId);
END
GO
