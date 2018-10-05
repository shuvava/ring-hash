IF db_id('EventsStore') is null
BEGIN
    CREATE DATABASE [EventsStore];
END
GO
USE [EventsStore]
GO
--select HashBytes('SHA2_256', 'Hello World!')
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
if not exists (select *
from sysobjects
where name='EventStore' and xtype='U')
BEGIN

    CREATE TABLE [dbo].[EventStore]
    (
        [CreateTime] [datetime] NOT NULL,
        [Id] [int] NOT NULL,
        [EventTime] [datetime] NOT NULL,
        [UserId] [int] NOT NULL,
        [TransactionId] [int] NOT NULL,
        [EventData] [nvarchar](4000) NOT NULL
    )
    CREATE CLUSTERED INDEX [IX_EventStore] ON [dbo].[EventStore]
    (
        [CreateTime] ASC
    )WITH (
        PAD_INDEX = OFF,
        STATISTICS_NORECOMPUTE = OFF,
        SORT_IN_TEMPDB = OFF,
        DROP_EXISTING = OFF,
        ONLINE = OFF,
        ALLOW_ROW_LOCKS = ON,
        ALLOW_PAGE_LOCKS = ON)
END
GO


/****** Object:  Index [IX_EventStore]    Script Date: 10/4/2018 7:02:51 PM ******/
GO



CREATE TYPE [dbo].[EventStoreTableType] AS TABLE(
    [CreateTime] [datetime] NOT NULL,
    [Id] [int] NOT NULL,
    [EventTime] [datetime] NOT NULL,
    [UserId] [int] NOT NULL,
    [TransactionId] [int] NOT NULL,
    [EventData] [nvarchar](4000) NOT NULL
        PRIMARY KEY CLUSTERED
(
	[CreateTime] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO

IF (OBJECT_ID('Events_Get') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[Events_Get] AS BEGIN SET NOCOUNT ON; END')
END
GO
-- =============================================
-- Author:		Name
-- Create date:
-- Description:
-- =============================================
ALTER PROCEDURE [dbo].[Events_Get]
    -- Add the parameters for the stored procedure here
    @dt datetime = 0,
    @filter int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1000)
        [CreateTime]
      , [Id]
      , [EventTime]
      , [UserId]
      , [TransactionId]
      , [EventData]
    FROM [EventsStore].[dbo].[EventStore] WITH(NOLOCK)
    WHERE
	CreateTime >@dt
END
GO


IF (OBJECT_ID('EventStore_Put') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[EventStore_Put] AS BEGIN SET NOCOUNT ON; END')
END
GO
ALTER PROCEDURE [dbo].[EventStore_Put]
    @events [EventStoreTableType] readonly
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[EventStore]
        ([CreateTime]
        ,[Id]
        ,[EventTime]
        ,[UserId]
        ,[TransactionId]
        ,[EventData])
    SELECT
        [CreateTime]
      , [Id]
      , [EventTime]
      , [UserId]
      , [TransactionId]
      , [EventData]
    FROM @events
    SELECT @@ROWCOUNT
END
GO
