USE [SupernodeScannerDB]
GO

/****** Object:  Table [dbo].[Accounts]    Script Date: 17/04/2017 23:42:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Accounts](
	[EncodedAddress] [varchar](60) NOT NULL,
	[OwnedByUser] [bigint] NOT NULL,
	[LastBlockHarvestedHeight] [bigint] NULL,
	[LastTransactionHash] [varchar](64) NULL,
	[CheckTxs] [bit] NOT NULL,
	[CheckBlocks] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EncodedAddress] ASC,
	[OwnedByUser] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[Accounts]  WITH CHECK ADD  CONSTRAINT [FK_AddressOwnedByUser] FOREIGN KEY([OwnedByUser])
REFERENCES [dbo].[Users] ([ChatId])
GO

ALTER TABLE [dbo].[Accounts] CHECK CONSTRAINT [FK_AddressOwnedByUser]
GO

