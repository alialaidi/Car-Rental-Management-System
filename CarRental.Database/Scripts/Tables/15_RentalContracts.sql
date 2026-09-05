-- =====================================================================
-- Table: RentalContracts
-- Purpose: Signed contract created when the customer picks up the car
--          (Step 6), closed on return (Step 8).
-- =====================================================================
IF OBJECT_ID('dbo.RentalContracts', 'U') IS NOT NULL DROP TABLE dbo.RentalContracts;
GO

CREATE TABLE dbo.RentalContracts
(
    ContractId              INT IDENTITY(1,1)  NOT NULL,
    ReservationId            INT                NULL,
    ContractNumber           NVARCHAR(50)       NOT NULL,
    CustomerId                INT                NOT NULL,
    VehicleId                 INT                NOT NULL,
    EmployeeId                 INT                NOT NULL,   -- employee who created the contract / handed over the car
    ReturnedByEmployeeId       INT                NULL,        -- employee who received the car back
    ActualPickupDate           DATETIME2(3)       NOT NULL,
    ActualReturnDate           DATETIME2(3)       NULL,
    PickupMileage              INT                NOT NULL,
    ReturnMileage              INT                NULL,
    PickupFuelLevel            NVARCHAR(20)       NOT NULL,
    ReturnFuelLevel            NVARCHAR(20)       NULL,
    ConditionNotes             NVARCHAR(1000)     NULL,
    RentalDays                 INT                NOT NULL,
    BasicRentalPrice           DECIMAL(18,2)      NOT NULL,
    DiscountAmount             DECIMAL(18,2)      NOT NULL CONSTRAINT DF_RentalContracts_Discount DEFAULT (0),
    AdditionalFees              DECIMAL(18,2)      NOT NULL CONSTRAINT DF_RentalContracts_AddFees DEFAULT (0),
    FuelCharges                 DECIMAL(18,2)      NOT NULL CONSTRAINT DF_RentalContracts_FuelCharges DEFAULT (0),
    DamageCharges                DECIMAL(18,2)      NOT NULL CONSTRAINT DF_RentalContracts_DamageCharges DEFAULT (0),
    TotalAmount                  DECIMAL(18,2)      NOT NULL CONSTRAINT DF_RentalContracts_TotalAmount DEFAULT (0),
    Status                       NVARCHAR(20)       NOT NULL CONSTRAINT DF_RentalContracts_Status DEFAULT ('Active'),

    CONSTRAINT PK_RentalContracts PRIMARY KEY CLUSTERED (ContractId),
    CONSTRAINT UQ_RentalContracts_ContractNumber UNIQUE (ContractNumber),
    CONSTRAINT UQ_RentalContracts_ReservationId UNIQUE (ReservationId),
    CONSTRAINT FK_RentalContracts_Reservations FOREIGN KEY (ReservationId) REFERENCES dbo.Reservations (ReservationId),
    CONSTRAINT FK_RentalContracts_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_RentalContracts_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId),
    CONSTRAINT FK_RentalContracts_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees (EmployeeId),
    CONSTRAINT FK_RentalContracts_ReturnedByEmployee FOREIGN KEY (ReturnedByEmployeeId) REFERENCES dbo.Employees (EmployeeId),
    CONSTRAINT CK_RentalContracts_Status CHECK (Status IN ('Active','Completed','Cancelled')),
    CONSTRAINT CK_RentalContracts_RentalDays CHECK (RentalDays > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_RentalContracts_CustomerId ON dbo.RentalContracts (CustomerId);
CREATE NONCLUSTERED INDEX IX_RentalContracts_VehicleId ON dbo.RentalContracts (VehicleId);
CREATE NONCLUSTERED INDEX IX_RentalContracts_Status ON dbo.RentalContracts (Status);
GO
