CREATE TABLE [dbo].[stores] (
    [stor_id]      CHAR (4)     NOT NULL,
    [stor_name]    VARCHAR (40) NULL,
    [stor_address] VARCHAR (40) NULL,
    [city]         VARCHAR (20) NULL,
    [state]        CHAR (2)     NULL,
    [zip]          CHAR (5)     NULL,
    CONSTRAINT [UPK_storeid] PRIMARY KEY CLUSTERED ([stor_id] ASC)
);

