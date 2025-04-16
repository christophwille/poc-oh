-- Table with data in it
CREATE TABLE SampleData (
    id INT PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    extradata NVARCHAR(MAX) NULL
);
GO

INSERT INTO SampleData (id, name, extradata)
VALUES (1, 'Test Entry', 'Some extra information');

ALTER TABLE SampleData 
ALTER COLUMN extradata NVARCHAR(MAX) NOT NULL;
GO

SELECT TOP (1000) [id]
      ,[name]
      ,[extradata]
  FROM [Playground].[dbo].[SampleData]
GO

DROP TABLE SampleData
GO







-- Table with realistic data in it
CREATE TABLE SampleData (
    id INT PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    extradata NVARCHAR(MAX) NULL
);
GO

INSERT INTO SampleData (id, name, extradata)
VALUES (1, 'Test Entry', 'Some extra information');
INSERT INTO SampleData (id, name)
VALUES (2, 'Another Entry');

SELECT TOP (1000) [id]
      ,[name]
      ,[extradata]
  FROM [Playground].[dbo].[SampleData]
GO

ALTER TABLE SampleData 
ALTER COLUMN extradata NVARCHAR(MAX) NOT NULL;
GO







-- Before making the column non-nullable, we need to ensure no NULL values exist
UPDATE SampleData 
SET extradata = 'Default Value' 
WHERE extradata IS NULL;

