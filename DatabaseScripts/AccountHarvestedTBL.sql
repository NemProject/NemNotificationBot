USE [SupernodeScannerDB]
GO

/****** Object:  Table [dbo].[AccountHarvestedSummary]    Script Date: 17/04/2017 23:42:37 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AccountHarvestedSummary](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MonitoredAccount] [varchar](60) NOT NULL,
	[OwnedByUser] [bigint] NOT NULL,
	[DateOfInput] [datetime] NOT NULL,
	[FeesEarned] [bigint] NOT NULL,
	[BlockHeight] [bigint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[AccountHarvestedSummary] ADD  DEFAULT (getdate()) FOR [DateOfInput]
GO

ALTER TABLE [dbo].[AccountHarvestedSummary] ADD  DEFAULT ((0)) FOR [FeesEarned]
GO

ALTER TABLE [dbo].[AccountHarvestedSummary] ADD  DEFAULT ((0)) FOR [BlockHeight]
GO

ALTER TABLE [dbo].[AccountHarvestedSummary]  WITH CHECK ADD  CONSTRAINT [FK_HarvestedAccount] FOREIGN KEY([MonitoredAccount], [OwnedByUser])
REFERENCES [dbo].[Accounts] ([EncodedAddress], [OwnedByUser])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[AccountHarvestedSummary] CHECK CONSTRAINT [FK_HarvestedAccount]
GO

