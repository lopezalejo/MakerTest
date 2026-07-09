IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SolidarityGrid')
BEGIN
    CREATE DATABASE SolidarityGrid;
END
GO
