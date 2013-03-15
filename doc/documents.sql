USE [Hydrospanner]
GO

/****** Object:  Table [dbo].[documents]    Script Date: 03/15/2013 14:20:55 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[documents](
	[identifier] [varchar](256) NOT NULL,
	[message_sequence] [bigint] NOT NULL,
	[document_hash] [int] NOT NULL,
	[document] [varbinary](max) NULL,
 CONSTRAINT [PK_documents] PRIMARY KEY CLUSTERED 
(
	[identifier] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


