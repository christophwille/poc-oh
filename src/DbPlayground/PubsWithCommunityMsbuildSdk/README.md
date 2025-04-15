# MSBuild.Sdk.SqlProj

Origin story: https://jmezach.github.io/post/introducing-msbuild-sdk-sqlproj/

Current intro: https://erikej.github.io/dotnet/dacfx/azuresql/2025/01/14/dacfx-msbuild-sdk-sqlproj-3.html

Docs: https://github.com/rr-wfm/MSBuild.Sdk.SqlProj/blob/master/README.md


## Steps

`dotnet new sqlproj`

`sqlpackage /Action:Extract /Properties:ExtractTarget=Flat /SourceConnectionString:"Data Source=.;Initial Catalog=pubs;Integrated Security=true;Encrypt=True;TrustServerCertificate=True" /TargetFile:Tables`


