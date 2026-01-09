IF DB_ID('BankMoreAccounts') IS NULL
BEGIN
    CREATE DATABASE [BankMoreAccounts];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'bankmore_user')
BEGIN
    CREATE LOGIN [bankmore_user]
    WITH PASSWORD = '$(APP_PASSWORD)',
         CHECK_POLICY = ON;
END
GO

USE [BankMoreAccounts];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'bankmore_user')
BEGIN
    CREATE USER [bankmore_user] FOR LOGIN [bankmore_user];
END
GO

IF IS_ROLEMEMBER('db_datareader', 'bankmore_user') <> 1
    ALTER ROLE db_datareader ADD MEMBER [bankmore_user];
GO

IF IS_ROLEMEMBER('db_datawriter', 'bankmore_user') <> 1
    ALTER ROLE db_datawriter ADD MEMBER [bankmore_user];
GO

IF IS_ROLEMEMBER('db_ddladmin', 'bankmore_user') <> 1
    ALTER ROLE db_ddladmin ADD MEMBER [bankmore_user];
GO
