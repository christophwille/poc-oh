CREATE TABLE [dbo].[publishers] (
    [pub_id]   CHAR (4)     NOT NULL,
    [pub_name] VARCHAR (40) NULL,
    [city]     VARCHAR (20) NULL,
    [state]    CHAR (2)     NULL,
    [country]  VARCHAR (30) DEFAULT ('USA') NULL,
    CONSTRAINT [UPKCL_pubind] PRIMARY KEY CLUSTERED ([pub_id] ASC),
    CHECK ([pub_id]='1756' OR [pub_id]='1622' OR [pub_id]='0877' OR [pub_id]='0736' OR [pub_id]='1389' OR [pub_id] like '99[0-9][0-9]')
);

