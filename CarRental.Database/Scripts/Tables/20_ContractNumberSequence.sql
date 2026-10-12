-- Shared sequence for reservation based rental contract numbers.
IF OBJECT_ID(N'dbo.ContractNumberSequence', N'SO') IS NULL
    EXEC(N'CREATE SEQUENCE dbo.ContractNumberSequence AS BIGINT START WITH 1 INCREMENT BY 1;');
GO
