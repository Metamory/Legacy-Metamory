# Metamory - The Version Aware Storage System

See the [Wiki](https://github.com/metamory/metamory/wiki) for documentation

## Developer setup

In Metamory.Web, add a `Secrets.config` file:
```xml
<appSettings>
	<add key="StorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...==" />
</appSettings>
```
Copy the Azure Storage connectionstring from the Azure management portal.

## License and Copyright

This project is open sourced under the MIT Licence. See [LICENSE.txt](https://github.com/Metamory/Metamory/blob/master/LICENSE.txt) for details.

Copyright (c) 2016 Arjan Einbu
