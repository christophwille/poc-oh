# XSD

Location: https://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02/, but https://github.com/microsoft/DacFx/issues/606

# Tools

https://github.com/mganss/XmlSchemaClassGenerator

```
dotnet tool install --global dotnet-xscgen --version 2.1.1182
```

```
xscgen -n DacPacDeployer.Schema .\deploy-report.xsd
```