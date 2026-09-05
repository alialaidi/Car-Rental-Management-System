-- =====================================================================
-- Script: 00_CreateDatabase.sql
-- Purpose: Create the CarRentalDB database (run once).
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'CarRentalDB')
BEGIN
    CREATE DATABASE CarRentalDB;
END
GO

USE CarRentalDB;
GO
