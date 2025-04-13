CREATE TABLE [dbo].[titles] (
    [title_id]  [dbo].[tid]   NOT NULL,
    [title]     VARCHAR (80)  NOT NULL,
    [type]      CHAR (12)     DEFAULT ('UNDECIDED') NOT NULL,
    [pub_id]    CHAR (4)      NULL,
    [price]     MONEY         NULL,
    [advance]   MONEY         NULL,
    [royalty]   INT           NULL,
    [ytd_sales] INT           NULL,
    [notes]     VARCHAR (200) NULL,
    [pubdate]   DATETIME      DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [UPKCL_titleidind] PRIMARY KEY CLUSTERED ([title_id] ASC),
    FOREIGN KEY ([pub_id]) REFERENCES [dbo].[publishers] ([pub_id])
);


GO
CREATE NONCLUSTERED INDEX [titleind]
    ON [dbo].[titles]([title] ASC);

