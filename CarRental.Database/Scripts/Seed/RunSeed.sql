-- Run in SQLCMD mode after Tables/RunAll.sql.
-- Set AdminPasswordHash to a precomputed password hash from a secure local source.
:on error exit
:r .\01_Roles.sql
:r .\02_Lookups.sql
:r .\03_BootstrapBranchAndAdmin.sql

PRINT 'CarRentalDB baseline seed completed.';
GO
