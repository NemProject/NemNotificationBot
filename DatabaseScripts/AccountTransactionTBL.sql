USE [SupernodeScannerDB]
GO

/****** Object:  Table [dbo].[AccountTxSummary]    Script Date: 17/04/2017 23:43:12 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AccountTxSummary](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MonitoredAccount] [varchar](60) NOT NULL,
	[OwnedByUser] [bigint] NOT NULL,
	[DateOfInput] [datetime] NULL,
	[Sender] [varchar](60) NOT NULL,
	[Recipient] [varchar](60) NOT NULL,
	[AmountIn] [bigint] NOT NULL,
	[AmountOut] [bigint] NOT NULL,
	[BalanceAfterTx] [bigint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[AccountTxSummary] ADD  DEFAULT (getdate()) FOR [DateOfInput]
GO

ALTER TABLE [dbo].[AccountTxSummary] ADD  DEFAULT ((0)) FOR [AmountIn]
GO

ALTER TABLE [dbo].[AccountTxSummary] ADD  DEFAULT ((0)) FOR [AmountOut]
GO

ALTER TABLE [dbo].[AccountTxSummary] ADD  DEFAULT ((0)) FOR [BalanceAfterTx]
GO

ALTER TABLE [dbo].[AccountTxSummary]  WITH CHECK ADD  CONSTRAINT [FK_TxAccount] FOREIGN KEY([MonitoredAccount], [OwnedByUser])
REFERENCES [dbo].[Accounts] ([EncodedAddress], [OwnedByUser])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[AccountTxSummary] CHECK CONSTRAINT [FK_TxAccount]
GO

