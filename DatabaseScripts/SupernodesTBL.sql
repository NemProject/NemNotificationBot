USE [SupernodeScannerDB]
GO

/****** Object:  Table [dbo].[SuperNodes]    Script Date: 17/04/2017 23:42:12 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SuperNodes](
	[NodeID] [int] IDENTITY(1,1) NOT NULL,
	[IP] [varchar](50) NOT NULL,
	[LastTest] [int] NULL,
	[OwnedByUser] [bigint] NOT NULL,
	[DepositAddress] [varchar](60) NOT NULL,
	[SNodeID] [int] NOT NULL,
	[Alias] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NodeID] ASC,
	[OwnedByUser] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[SuperNodes]  WITH CHECK ADD  CONSTRAINT [FK_OwnedByUser] FOREIGN KEY([OwnedByUser])
REFERENCES [dbo].[Users] ([ChatId])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[SuperNodes] CHECK CONSTRAINT [FK_OwnedByUser]
GO

