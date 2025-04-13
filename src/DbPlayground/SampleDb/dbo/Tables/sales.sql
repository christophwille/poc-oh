CREATE TABLE [dbo].[sales] (
    [stor_id]  CHAR (4)     NOT NULL,
    [ord_num]  VARCHAR (20) NOT NULL,
    [ord_date] DATETIME     NOT NULL,
    [qty]      SMALLINT     NOT NULL,
    [payterms] VARCHAR (12) NOT NULL,
    [title_id] [dbo].[tid]  NOT NULL,
    CONSTRAINT [UPKCL_sales] PRIMARY KEY CLUSTERED ([stor_id] ASC, [ord_num] ASC, [title_id] ASC),
    FOREIGN KEY ([stor_id]) REFERENCES [dbo].[stores] ([stor_id]),
    FOREIGN KEY ([title_id]) REFERENCES [dbo].[titles] ([title_id])
);


GO
CREATE NONCLUSTERED INDEX [titleidind]
    ON [dbo].[sales]([title_id] ASC);

