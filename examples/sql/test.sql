USE [EventsStore]
GO
select count(*) from dbo.EventStore

exec [dbo].[Events_Get] @dt='2016-10-10 12:50:13.403',@filter=70


/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP (1000) [Hash]
      ,[WorkerId]
      ,[ThreadCheckpoint]
  FROM [EventsStore].[dbo].[EventThread]
  where Hash = 0
 -- truncate table [EventsStore].[dbo].[EventThread]
  -- exec [dbo].[EventThread_Checkpoint] @hash=0,@workerId=1,@threadCheckpoint='2018-09-30 01:03:31.183'