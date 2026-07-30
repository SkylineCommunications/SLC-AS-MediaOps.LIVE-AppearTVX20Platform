# SLC-AS-MediaOps.LIVE-AppearTVX20Platform
# Connection Handler - AppearTV X20 Platform

Connection handler script to integrate [AppearTV X20 Platform](https://catalog.dataminer.services/details/7bb327a7-0844-4c2b-b5bc-fbfd4e3bc8de) elements in the [MediaOps Live](https://catalog.dataminer.services/details/213031b9-af0b-488c-be20-934912b967c0) solution.

[![Catalog](https://img.shields.io/badge/Catalog-View-blue)](https://catalog.dataminer.services/details/d657be70-7523-4b99-b184-bff924461e69)

## Supported Functionality

### Connection Monitoring

The script subscribes to the **Dual IP Output Multiplex Service** table (PID 3100) of AppearTV X20 Platform elements and automatically detects connection changes:

- **New/updated rows**: Maps source and destination identifiers to MediaOps Live endpoints and registers the connections.
- **Deleted rows**: Detects disconnected destinations and registers the disconnection.

### Connect

Handles connect requests from MediaOps Live by:

1. Resolving source and destination endpoint identifiers (format: `[slot id].[key]`).
2. Looking up the source flow from the input configuration.
3. Updating the IP Gateway output on the AppearTV X20 Platform element with the new source flow via InterApp messages.

### Disconnect

Not yet implemented.

## NuGet Packages

| Package | Description |
|---|---|
| [Skyline.DataMiner.DataSources.Appear.X](https://www.nuget.org/packages/Skyline.DataMiner.DataSources.Appear.X) | Schema definitions and data source integration for the AppearTV X20 Platform connector. Includes the Connector API used to send InterApp messages to the element. |
| [Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live.Automation](https://www.nuget.org/packages/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live.Automation) | MediaOps Live connection handler base classes and API for registering connections, retrieving endpoints, and handling connect/disconnect requests. |
| [Skyline.DataMiner.Dev.Automation](https://www.nuget.org/packages/Skyline.DataMiner.Dev.Automation) | Core DataMiner Automation script development package. |
