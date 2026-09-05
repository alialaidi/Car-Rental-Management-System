-- =====================================================================
-- Table: Reservations
-- Purpose: Customer booking request (Step 4-5 of the workflow).
-- =====================================================================
IF OBJECT_ID('dbo.Reservations', 'U') IS NOT NULL DROP TABLE dbo.Reservations;
GO

CREATE TABLE dbo.Reservations
(
    ReservationId              INT IDENTITY(1,1)  NOT NULL,
    CustomerId                 INT                NOT NULL,
    VehicleId                  INT                NOT NULL,
    PickupBranchId              INT                NOT NULL,
    ReturnBranchId              INT                NOT NULL,
    PickupDateTime              DATETIME2(3)       NOT NULL,
    ExpectedReturnDateTime      DATETIME2(3)       NOT NULL,
    Status                      NVARCHAR(20)       NOT NULL CONSTRAINT DF_Reservations_Status DEFAULT ('Pending'),
    ConfirmedByEmployeeId       INT                NULL,
    CreatedAt                   DATETIME2(3)       NOT NULL CONSTRAINT DF_Reservations_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Reservations PRIMARY KEY CLUSTERED (ReservationId),
    CONSTRAINT FK_Reservations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Reservations_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId),
    CONSTRAINT FK_Reservations_PickupBranch FOREIGN KEY (PickupBranchId) REFERENCES dbo.Branches (BranchId),
    CONSTRAINT FK_Reservations_ReturnBranch FOREIGN KEY (ReturnBranchId) REFERENCES dbo.Branches (BranchId),
    CONSTRAINT FK_Reservations_ConfirmedByEmployee FOREIGN KEY (ConfirmedByEmployeeId) REFERENCES dbo.Employees (EmployeeId),
    CONSTRAINT CK_Reservations_Status CHECK (Status IN ('Pending','Confirmed','Rejected','Cancelled','Completed')),
    CONSTRAINT CK_Reservations_DatesOrder CHECK (ExpectedReturnDateTime > PickupDateTime)
);
GO

CREATE NONCLUSTERED INDEX IX_Reservations_VehicleId ON dbo.Reservations (VehicleId);
CREATE NONCLUSTERED INDEX IX_Reservations_CustomerId ON dbo.Reservations (CustomerId);
CREATE NONCLUSTERED INDEX IX_Reservations_Status ON dbo.Reservations (Status);
-- Supports the "overlapping reservation" check done in the SP layer:
CREATE NONCLUSTERED INDEX IX_Reservations_Vehicle_DateRange
    ON dbo.Reservations (VehicleId, PickupDateTime, ExpectedReturnDateTime)
    INCLUDE (Status);
GO
