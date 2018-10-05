USE [EventsStore]
GO
--select HashBytes('SHA2_256', 'Hello World!')
/****** Object:  Table [dbo].[EventStore]    Script Date: 10/4/2018 6:28:11 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EventStore](
	[CreateTime] [datetime] NOT NULL,
	[Id] [int] NOT NULL,
	[EventTime] [datetime] NOT NULL,
	[UserId] [int] NOT NULL,
	[TransactionId] [int] NOT NULL,
	[EventData] [nvarchar](4000) NOT NULL
) ON [PRIMARY]
GO
USE [EventsStore]
GO

/****** Object:  Index [IX_EventStore]    Script Date: 10/4/2018 7:02:51 PM ******/
CREATE CLUSTERED INDEX [IX_EventStore] ON [dbo].[EventStore]
(
	[CreateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
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
USE [EventsStore]
GO

/****** Object:  StoredProcedure [dbo].[Events_Get]    Script Date: 10/4/2018 7:03:33 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Name
-- Create date: 
-- Description:	
-- =============================================
CREATE PROCEDURE [dbo].[Events_Get] 
	-- Add the parameters for the stored procedure here
	@dt datetime = 0, 
	@filter int = 0
AS
BEGIN
	SET NOCOUNT ON;
SELECT TOP (1000) [CreateTime]
      ,[Id]
      ,[EventTime]
      ,[UserId]
      ,[TransactionId]
      ,[EventData]
  FROM [EventsStore].[dbo].[EventStore] WITH(NOLOCK)
  WHERE
	CreateTime >@dt
END
GO



CREATE PROCEDURE [dbo].[EventStore_Put]
	@events [EventStoreTableType] readonly
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [dbo].[EventStore] ([CreateTime]
      ,[Id]
      ,[EventTime]
      ,[UserId]
      ,[TransactionId]
      ,[EventData])
	SELECT 
	[CreateTime]
      ,[Id]
      ,[EventTime]
      ,[UserId]
      ,[TransactionId]
      ,[EventData]
	FROM @events
END
GO
