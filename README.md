# Metamory - The Version Aware Storage System

See the [Wiki](https://github.com/metamory/metamory/wiki) for documentation

# Developer setup

In Metamory.Web folder, from the command line run:
```
bower update
````

In Metamory.Web, add a `Secrets.config` file:
```xml
<appSettings>
	<add key="StorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...==" />
</appSettings>
```
(Copy the Azure Storage connectionstring from the Azure management console.)
