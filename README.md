# EventR

TBD

## Local NuGet config
**nuget.config** file at the root of the project
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="D:\LocalNuGetPackages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Than you can publish core EventR packages locally by `eventr\build.ps1 -PublishLocally` and used them within the json solution without going through nuget.org.