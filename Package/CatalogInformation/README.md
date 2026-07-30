# Connection Handler - AppearTV X20 Platform

## About

This Automation script acts as a connection handler to integrate elements using the [AppearTV X20 Platform](https://catalog.dataminer.services/details/7bb327a7-0844-4c2b-b5bc-fbfd4e3bc8de) connector with the MediaOps Live solution.

> [!INFO]
> For general information about connection handler scripts in MediaOps Live, see [Connection Handler Script](https://docs.dataminer.services/solutions/standard_solutions/MediaOps/MediaOps.Live/Mediation_layer/MO_ConnectionHandlerScript.html).

## Key Features

### Dual IP Output Multiplex Service Table

- Subscribes to the Dual IP Output Multiplex Service table and automatically updates connections in MediaOps Live on changes.
- Supports connecting IP Gateway Output to a source from MediaOps Live.

> [!NOTE]
> Additional features can be supported by contributing directly to the [script repository](https://github.com/SkylineCommunications/SLC-AS-MediaOps.LIVE-AppearTVX20Platform).

## Prerequisites

- One or more elements running the [AppearTV X20 Platform](https://catalog.dataminer.services/details/7bb327a7-0844-4c2b-b5bc-fbfd4e3bc8de) connector.
- DataMiner system with the [MediaOps Live](https://catalog.dataminer.services) solution installed.
- Endpoints configured in MediaOps Live linked to the AppearTV X20 Platform elements with Identifier defined as "[slot id].[key]".