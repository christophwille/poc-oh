# Notes

Built based on the article:

https://www.kenmuse.com/blog/devops-sql-server-dacpac-docker

## Create yourself a bacpac

`dotnet tool install --global Microsoft.SqlPackage --version 170.2.70`

Eg 

`SqlPackage /Action:Export /TargetFile:"SampleDb.bacpac" /SourceConnectionString:"Server=.;Integrated Security=SSPI;Initial Catalog=pubs;Encrypt=True;TrustServerCertificate=True;"`


## Build the container image

`docker build --build-arg PASSWORD=YourS3cureP@ass --build-arg DBNAME=pubs -t pubs:1.0 .`

## Run the container

`docker run -p 10109:1433 --name pubsdb -d pubs:1.0`

