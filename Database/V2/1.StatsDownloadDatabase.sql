USE [master]
GO
/****** Object:  Database [FoldingCash]    Script Date: 6/29/2019 4:00:40 PM ******/
CREATE DATABASE [FoldingCash]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'FoldingCash', FILENAME = N'C:\Databases\FoldingCash\FoldingCash.mdf' , SIZE = 10240KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
 LOG ON 
( NAME = N'FoldingCash_log', FILENAME = N'C:\Databases\FoldingCash\FoldingCash_log.ldf' , SIZE = 1280KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [FoldingCash] SET COMPATIBILITY_LEVEL = 110
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [FoldingCash].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [FoldingCash] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [FoldingCash] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [FoldingCash] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [FoldingCash] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [FoldingCash] SET ARITHABORT OFF 
GO
ALTER DATABASE [FoldingCash] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [FoldingCash] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [FoldingCash] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [FoldingCash] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [FoldingCash] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [FoldingCash] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [FoldingCash] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [FoldingCash] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [FoldingCash] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [FoldingCash] SET  DISABLE_BROKER 
GO
ALTER DATABASE [FoldingCash] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [FoldingCash] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [FoldingCash] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [FoldingCash] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [FoldingCash] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [FoldingCash] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [FoldingCash] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [FoldingCash] SET RECOVERY FULL 
GO
ALTER DATABASE [FoldingCash] SET  MULTI_USER 
GO
ALTER DATABASE [FoldingCash] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [FoldingCash] SET DB_CHAINING OFF 
GO
ALTER DATABASE [FoldingCash] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [FoldingCash] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [FoldingCash] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'FoldingCash', N'ON'
GO