-- Grant the Windows service account (runs as LocalSystem) access to the local dev database.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NT AUTHORITY\SYSTEM')
    CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;
GO
USE [LivestockManager];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NT AUTHORITY\SYSTEM')
    CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
GO
ALTER ROLE db_owner ADD MEMBER [NT AUTHORITY\SYSTEM];
GO
PRINT 'Grant complete: NT AUTHORITY\SYSTEM is db_owner on LivestockManager';
