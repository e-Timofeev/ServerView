USE [master]
GO

/****** Object:  Create database [ServersList] ******/
CREATE DATABASE [ServersList]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'ServersList', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA\ServersList.mdf' , SIZE = 5120KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'ServersList_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA\ServersList_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [ServersList] SET COMPATIBILITY_LEVEL = 110
GO

ALTER DATABASE [ServersList] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [ServersList] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [ServersList] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [ServersList] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [ServersList] SET ARITHABORT OFF 
GO

ALTER DATABASE [ServersList] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [ServersList] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [ServersList] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [ServersList] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [ServersList] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [ServersList] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [ServersList] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [ServersList] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [ServersList] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [ServersList] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [ServersList] SET  DISABLE_BROKER 
GO

ALTER DATABASE [ServersList] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [ServersList] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [ServersList] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [ServersList] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [ServersList] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [ServersList] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [ServersList] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [ServersList] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [ServersList] SET  MULTI_USER 
GO

ALTER DATABASE [ServersList] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [ServersList] SET DB_CHAINING OFF 
GO

ALTER DATABASE [ServersList] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO

ALTER DATABASE [ServersList] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO

ALTER DATABASE [ServersList] SET  READ_WRITE 
GO

/****** Object:  Create login [server_admin] ******/
CREATE LOGIN [server_admin] WITH PASSWORD=N'123qweASD', DEFAULT_DATABASE=[ServersList], DEFAULT_LANGUAGE=[русский], CHECK_EXPIRATION=OFF, CHECK_POLICY=ON
GO
ALTER LOGIN [server_admin] ENABLE
GO

/****** Preparation of the base for work ******/
USE [ServersList]
GO
/****** Object: Create user [server_admin] ******/
CREATE USER [server_admin] FOR LOGIN [server_admin] WITH DEFAULT_SCHEMA=[dbo]
ALTER ROLE db_owner ADD MEMBER [server_admin]
GO

/****** Object:  Create table [dbo].[ArchiveList] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ArchiveList](
	[ID] [tinyint] IDENTITY(1,1) NOT NULL,
	[ParrentServerID] [tinyint] NOT NULL,
	[ServerName] [nvarchar](30) NULL,
	[IP] [nvarchar](15) NULL,
	[Domain] [nvarchar](25) NULL,
	[Description] [nvarchar](500) NULL
) ON [PRIMARY]
GO

/****** Object:  Create table [dbo].[PhyServers] ******/
CREATE TABLE [dbo].[PhyServers](
	[ID] [tinyint] IDENTITY(1,1) NOT NULL,
	[ServerName] [nvarchar](30) NULL,
	[IP] [nvarchar](15) NULL,
	[Domain] [nvarchar](25) NULL,
	[Description] [nvarchar](500) NULL,
	[Status] [image] NULL,
	[Arhive] [tinyint] NULL,
 CONSTRAINT [PK_PhyServers] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** Object:  Create table [dbo].[VirServers] ******/
CREATE TABLE [dbo].[VirServers](
	[ID] [tinyint] IDENTITY(1,1) NOT NULL,
	[ParrentServerID] [tinyint] NOT NULL,
	[ServerName] [nvarchar](30) NULL,
	[IP] [nvarchar](15) NULL,
	[Domain] [nvarchar](25) NULL,
	[Description] [nvarchar](500) NULL,
	[Status] [image] NULL,
	[Archive] [tinyint] NULL,
 CONSTRAINT [PK_VirServers] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[VirServers]  WITH CHECK ADD  CONSTRAINT [FK_VirServers_PhyServers] FOREIGN KEY([ParrentServerID])
REFERENCES [dbo].[PhyServers] ([ID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[VirServers] CHECK CONSTRAINT [FK_VirServers_PhyServers]
GO

/****** Object:  Creaye StoredProcedure [dbo].[CHECKIDENT] ******/
-- =============================================
-- Author:		Timofeev E.A.
-- Create date: 20170711
-- Description:	—брос счетчиков первичного ключа
-- =============================================
CREATE PROCEDURE [dbo].[CHECKIDENT]
AS
BEGIN
		DBCC CHECKIDENT (PhyServers,  RESEED, 0)
		DBCC CHECKIDENT (VirServers,  RESEED, 0)
END
GO

/****** Object:  Create StoredProcedure [dbo].[DeleteNoActiveInVirServers] ******/
CREATE PROCEDURE [dbo].[DeleteNoActiveInVirServers]
AS
BEGIN

DELETE FROM [dbo].[VirServers]
      WHERE dbo.VirServers.Archive = 1
End
GO

/****** Object:  Create StoredProcedure [dbo].[MoveNoActiveVM] *****/
CREATE PROCEDURE [dbo].[MoveNoActiveVM]
as

INSERT INTO [dbo].[ArchiveList]
           ([ParrentServerID]
           ,[ServerName]
           ,[IP]
           ,[Domain]
           ,[Description])

SELECT 
       [ParrentServerID]
      ,[ServerName]
      ,[IP]
      ,[Domain]
      ,[Description]
FROM dbo.VirServers
Where VirServers.Archive = 1
GO