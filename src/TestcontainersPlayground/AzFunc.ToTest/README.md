# Notes

Created using https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container `func init --docker`

## Sample endpoints

* http://localhost:7201/api/SampleHttpFunctionNoDeps

## .NET Docker Container Publish and Test

* Publish `dotnet publish -c Release --os linux --arch x64 /t:PublishContainer -p ContainerArchiveOutputPath="./sample-funcs.tar.gz" -p ContainerImageTag="4711"`
* Load into Docker `docker load --input "sample-funcs.tar.gz"`
* Run interactively `docker run -it -p 7071:80 sample-funcs:4711`

If env vars are needed insert -e  `envvar="valueofenvvar"`
