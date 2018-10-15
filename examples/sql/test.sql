USE [EventsStore]
GO
select count(*) from dbo.EventStore

exec [dbo].[Events_Get] @dt='2016-10-10 12:50:13.403',@filter=70


/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP (2000) [Hash]
      ,[WorkerId]
      ,[ThreadCheckpoint], GETUTCDATE()
  FROM [EventsStore].[dbo].[EventThread]
  where Hash = 8
  order by [ThreadCheckpoint]

 -- truncate table [EventsStore].[dbo].[EventThread]
  -- exec [dbo].[EventThread_Checkpoint] @hash=0,@workerId=1,@threadCheckpoint='2018-09-30 01:03:31.183'
  exec EventsStore.[dbo].Workers_Checkpoint 2
/*
	UPDATE dbo.Workers
		SET [LastCheckpointTime] = DATEADD(minute, -3, GETUTCDATE())
	WHERE	
		[Id] = 2
*/

  declare @dt datetime = DATEADD(minute, -3, GETUTCDATE())
  exec EventsStore.[dbo].[Workers_Get] @dt

  insert  into [dbo].[EventStore]  
  VALUES (GETUTCDATE(), 8,GETUTCDATE(),8,8,'test1')

  select CHECKSUM(8) % 1024, 8 % 1024