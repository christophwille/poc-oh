# Notes

Created using https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container `func init --docker`

## Sample endpoints

* http://localhost:7201/api/SampleHttpFunctionNoDeps

## .NET Docker Container Publish and Test

* Publish `dotnet publish -c Release --os linux --arch x64 /t:PublishContainer -p ContainerArchiveOutputPath="./sample-funcs.tar.gz" -p ContainerImageTag="4711"`
* Load into Docker `docker load --input "sample-funcs.tar.gz"`
* Run interactively `docker run -it -p 7071:80 sample-funcs:4711`

If env vars are needed insert -e  `envvar="valueofenvvar"`

NOTE: https://github.com/microsoft/azure-container-apps/issues/1415 doesn't work in ACA!

## Slightly Non-Standard Dockerfile Approach

Default Dockerfile would be
```
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env

COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
mkdir -p /home/site/wwwroot && \
dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
```

That is, building and publishing inside with the SDK container. However, one can also do the following:
```
dotnet publish AzFunc.ToTest.csproj --output ./bin/pubout
```

And the Dockerfile then being only

```
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY ./bin/pubout /home/site/wwwroot
```

Instead of pushing to ACR, you also can create a tar.gz locally for artifact publishing (save however includes parent layers)

```
docker build --tag sample-funcs:V2 .
docker save sample-funcs:V2 -o sample-funcs.tar.gz
```
