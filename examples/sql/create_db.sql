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

if not exists (select * from sysobjects where name='EventStore' and xtype='U')
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

    CREATE TYPE [dbo].[EventStoreTableType] AS TABLE(
        [CreateTime] [datetime] NOT NULL,
        [Id] [int] NOT NULL,
        [EventTime] [datetime] NOT NULL,
        [UserId] [int] NOT NULL,
        [TransactionId] [int] NOT NULL,
        [EventData] [nvarchar](4000) NOT NULL
            PRIMARY KEY CLUSTERED
    (
        [CreateTime] ASC,
		[Id]
    )WITH (IGNORE_DUP_KEY = OFF)
    )
END
GO

if not exists (select * from sysobjects where name='Workers' and xtype='U')
BEGIN
    CREATE TABLE dbo.Workers (
        [Id] [int] PRIMARY KEY,
		[LockExpirationTime] [datetime] NOT NULL,
        [Description] [nvarchar](150) NULL
    )
END
GO

if not exists (select * from sysobjects where name='EventThread' and xtype='U')
BEGIN
    CREATE TABLE dbo.EventThread (
        [Hash] [int] PRIMARY KEY,
        [WorkerId] [int] NOT NULL,
        [Checkpoint] [datetime] NOT NULL,
		[LockExpirationTime] [datetime] NOT NULL
    )
END
GO

IF (OBJECT_ID('EventThread_Checkpoint') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[EventThread_Checkpoint] AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE [dbo].EventThread_Checkpoint(
    @hash int,
	@workerId int,
	@Checkpoint [datetime],
	@LockExpirationTime [datetime]
)
AS
BEGIN
    SET NOCOUNT ON;
	declare @count int 
	UPDATE dbo.EventThread
		SET [Checkpoint] = @Checkpoint, 
		[LockExpirationTime] = @LockExpirationTime
	WHERE
		[Hash] = @hash AND
		[WorkerId] = @workerId AND
		[Checkpoint]<= @Checkpoint;
	set @count =@@ROWCOUNT
	if @count = 0 
	BEGIN
		if NOT EXISTS(select top 1 1 from dbo.EventThread WITH(NOLOCK) WHERE [Hash] = @hash)
		BEGIN
			 INSERT dbo.EventThread ([Hash], [WorkerId], [Checkpoint], [LockExpirationTime])
			 VALUES (@hash, @workerId, @Checkpoint, @LockExpirationTime)
			 set @count =@@ROWCOUNT
		END ELSE BEGIN
			UPDATE dbo.EventThread
				SET [LockExpirationTime] = @LockExpirationTime
			WHERE
				[Hash] = @hash AND
				[WorkerId] = @workerId
		END
	END

	SELECT @count
END
GO

IF (OBJECT_ID('EventThread_Update') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[EventThread_Update] AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE [dbo].EventThread_Update(
    @hash int,
	@oldWorkerId int,
	@newWorkerId int,
	@LockExpirationTime [datetime]
)
AS
BEGIN
    SET NOCOUNT ON;
	UPDATE dbo.EventThread
		SET [WorkerId] = @newWorkerId, 
		[LockExpirationTime] = @LockExpirationTime
	WHERE
		[Hash] = @hash AND
		[WorkerId] = @oldWorkerId;
	SELECT @@ROWCOUNT
END
GO

IF (OBJECT_ID('EventThread_Get') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[EventThread_Get] AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE [dbo].EventThread_Get(
    @hash int
)
AS
BEGIN
    SET NOCOUNT ON;
	SELECT [Hash], [WorkerId], [Checkpoint], [LockExpirationTime]
	FROM dbo.EventThread WITH(NOLOCK)
	WHERE
		[Hash] = @hash
END
GO

IF (OBJECT_ID('Workers_Put') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[Workers_Put] AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE [dbo].Workers_Put(
    @Id int,
    @Description nvarchar(150),
	@LockExpirationTime [datetime]
)
AS
BEGIN
    SET NOCOUNT ON;
	UPDATE dbo.Workers
		SET [Description] = @Description,
		[LockExpirationTime] = @LockExpirationTime
	WHERE 
		[Id] = @Id
	IF @@ROWCOUNT = 0 
	BEGIN	
		INSERT INTO dbo.Workers ([Id], [LockExpirationTime], [Description])
		values (@Id, @LockExpirationTime, @Description)
	END
END
GO

IF (OBJECT_ID('Workers_Checkpoint') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[Workers_Checkpoint] AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE [dbo].Workers_Checkpoint(
	@Id int,
	@LockExpirationTime [datetime]
)
AS
BEGIN
    SET NOCOUNT ON;
	UPDATE dbo.Workers
		SET [LockExpirationTime] = @LockExpirationTime
	WHERE	
		[Id] = @Id
END
GO

IF (OBJECT_ID('Workers_Get') IS NULL)
BEGIN
    exec('CREATE PROCEDURE [dbo].[Workers_Get] AS BEGIN SET NOCOUNT ON; END')
END
GO
ALTER PROCEDURE [dbo].[Workers_Get]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        [Id]
	  , [LockExpirationTime]
      , [Description]
    FROM [EventsStore].[dbo].[Workers] WITH(NOLOCK)
    WHERE
		[LockExpirationTime] > GETUTCDATE()
END
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
    SELECT TOP (10000)
        [CreateTime]
      , [Id]
      , [EventTime]
      , [UserId]
      , [TransactionId]
      , [EventData]
    FROM [EventsStore].[dbo].[EventStore] WITH(NOLOCK)
    WHERE
	CreateTime >@dt 
	--AND CHECKSUM([UserId]) % 1024 = @filter
	AND [UserId] % 1024 = @filter
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
