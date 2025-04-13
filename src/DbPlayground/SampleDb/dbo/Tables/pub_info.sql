CREATE TABLE [dbo].[pub_info] (
    [pub_id]  CHAR (4) NOT NULL,
    [logo]    IMAGE    NULL,
    [pr_info] TEXT     NULL,
    CONSTRAINT [UPKCL_pubinfo] PRIMARY KEY CLUSTERED ([pub_id] ASC),
    FOREIGN KEY ([pub_id]) REFERENCES [dbo].[publishers] ([pub_id])
);

