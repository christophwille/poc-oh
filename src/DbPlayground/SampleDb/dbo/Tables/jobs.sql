CREATE TABLE [dbo].[jobs] (
    [job_id]   SMALLINT     IDENTITY (1, 1) NOT NULL,
    [job_desc] VARCHAR (50) DEFAULT ('New Position - title not formalized yet') NOT NULL,
    [min_lvl]  TINYINT      NOT NULL,
    [max_lvl]  TINYINT      NOT NULL,
    PRIMARY KEY CLUSTERED ([job_id] ASC),
    CHECK ([max_lvl]<=(250)),
    CHECK ([min_lvl]>=(10))
);

