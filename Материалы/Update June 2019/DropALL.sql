USE [master]
GO

/****** Object:  Database [EcmServerList] ******/
ALTER DATABASE ServersList 
SET SINGLE_USER 
WITH ROLLBACK IMMEDIATE;
GO
DROP DATABASE [ServersList]
GO

/****** Object:  Login [server_admin] ******/
DROP LOGIN [server_admin]
GO