# Usage Differences to xUnit

See https://github.com/coverlet-coverage/coverlet/issues/1838#issuecomment-4080898629

Exclude approach:

`dotnet test --coverlet --coverlet-output-format cobertura --coverlet-exclude [NUnit3.*]*`

Include only stuff that is relevant:

`dotnet test --coverlet --coverlet-output-format cobertura --coverlet-include [MTPCoverlet*]*`

Exclude NUnit3 isn't sufficient, see https://github.com/coverlet-coverage/coverlet/issues/1838#issuecomment-4081398964

## Report Generation

https://github.com/danielpalme/ReportGenerator

`dotnet tool install -g dotnet-reportgenerator-globaltool`

With settings generated using https://reportgenerator.io/usage

`reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport`