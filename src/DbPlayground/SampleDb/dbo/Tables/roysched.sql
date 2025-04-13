CREATE TABLE [dbo].[roysched] (
    [title_id] [dbo].[tid] NOT NULL,
    [lorange]  INT         NULL,
    [hirange]  INT         NULL,
    [royalty]  INT         NULL,
    FOREIGN KEY ([title_id]) REFERENCES [dbo].[titles] ([title_id])
);


GO
CREATE NONCLUSTERED INDEX [titleidind]
    ON [dbo].[roysched]([title_id] ASC);

