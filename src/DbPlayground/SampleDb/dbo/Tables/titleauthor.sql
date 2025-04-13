CREATE TABLE [dbo].[titleauthor] (
    [au_id]      [dbo].[id]  NOT NULL,
    [title_id]   [dbo].[tid] NOT NULL,
    [au_ord]     TINYINT     NULL,
    [royaltyper] INT         NULL,
    CONSTRAINT [UPKCL_taind] PRIMARY KEY CLUSTERED ([au_id] ASC, [title_id] ASC),
    FOREIGN KEY ([au_id]) REFERENCES [dbo].[authors] ([au_id]),
    FOREIGN KEY ([title_id]) REFERENCES [dbo].[titles] ([title_id])
);


GO
CREATE NONCLUSTERED INDEX [auidind]
    ON [dbo].[titleauthor]([au_id] ASC);


GO
CREATE NONCLUSTERED INDEX [titleidind]
    ON [dbo].[titleauthor]([title_id] ASC);

