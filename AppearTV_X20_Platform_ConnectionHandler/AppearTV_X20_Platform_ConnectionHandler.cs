namespace AppearTVX20PlatformConnectionHandler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.ConnectorAPI.Appear.X;
    using Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls;
    using Skyline.DataMiner.DataSources.Appear.X.Schema;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.IpGateway.V1_31.Output.Command;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.Types.Output.V1_29.Type;
    using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
    using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Mediation.ConnectionHandlers;
    using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers.Data;
    using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script : ConnectionHandlerScript
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,
            Converters = new List<JsonConverter> { new StringEnumConverter() },
        };

        public override IEnumerable<ElementInfo> GetSupportedElements(IEngine engine, IEnumerable<ElementInfo> elements)
        {
            return elements.Where(e => e.Protocol == "AppearTV X20 Platform");
        }

        public override IEnumerable<SubscriptionInfo> GetSubscriptionInfo(IEngine engine)
        {
            return new[]
            {
                new SubscriptionInfo(SubscriptionInfo.ParameterType.Table, 3100), // Dual IP IP Output Multiplex Service Table
			};
        }

        public override void ProcessParameterUpdate(IEngine engine, IConnectionHandlerEngine connectionEngine, ParameterUpdate update)
        {
            if (update.ParameterId != 3100)
            {
                return;
            }

            var updatedConnections = new List<ConnectionUpdate>();

            if (update.UpdatedRows != null)
            {
                foreach (var updatedRow in update.UpdatedRows.Values)
                {
                    var key = Convert.ToString(updatedRow[1]); // destination endpoint identifier
                    var sourceKey = Convert.ToString(updatedRow[20]); // source endpoint identifier

                    var endpoints = connectionEngine.Api.Endpoints.GetByElementAndIdentifiers(update.DmsElementId, new[] { key, sourceKey });
                    
                    var destinationEndpoint = endpoints.FirstOrDefault(e => e.Identifier == key);
                    if (destinationEndpoint == null)
                    {
                        continue;
                    }

                    var sourceEndpoint = endpoints.FirstOrDefault(e => e.Identifier == sourceKey);
                    if (sourceEndpoint == null)
                    {
                        // unknown connection
                        updatedConnections.Add(new ConnectionUpdate(destinationEndpoint, isConnected: true));
                    }
                    else
                    {
                        // connection between source and destination
                        updatedConnections.Add(new ConnectionUpdate(sourceEndpoint, destinationEndpoint));
                    }
                }
            }

            if (update.DeletedRows != null)
            {
                // TODO: verify if other rows need to be verified as another service could be connected to this output?
                foreach (var deletedRow in update.DeletedRows.Values)
                {
                    var key = Convert.ToString(deletedRow[1]); // destination endpoint identifier
                    var destinationEndpoint = connectionEngine.Api.Endpoints.GetByRoleElementAndIdentifier(EndpointRole.Destination, update.DmsElementId, key);
                    if (destinationEndpoint == null)
                    {
                        continue;
                    }

                    // disconnect destination
                    updatedConnections.Add(new ConnectionUpdate(destinationEndpoint, isConnected: false));
                }
            }

            if (updatedConnections.Count > 0)
            {
                connectionEngine.RegisterConnections(updatedConnections);
            }
        }

        public override void Connect(IEngine engine, IConnectionHandlerEngine connectionEngine, CreateConnectionsRequest createConnectionsRequest)
        {
            var groupedByDestinationElement = createConnectionsRequest.Connections.GroupBy(x => x.DestinationEndpoint.Element);
            foreach (var group in groupedByDestinationElement)
            {
                var elementId = group.Key.Value;
                var element = engine.FindElement(elementId.AgentId, elementId.ElementId);

                var appearElement = new AppearXElement(Engine.SLNetRaw, element.DmaId, element.ElementId) 
                {
                    Timeout = TimeSpan.FromSeconds(30),
                };

                foreach (var connection in group)
                {
                    var sourceEndpoint = connection.SourceEndpoint;

                    var destinationEndpoint = connection.DestinationEndpoint;
                    var destinationEndpointIdentifierParts = destinationEndpoint.Identifier.Split('.');
                    if (destinationEndpointIdentifierParts.Length != 2) 
                    {
                        engine.Log($"Destination endpoint identifier '{destinationEndpoint.Identifier}' is not in correct format. It should be '[slot id].[output key]'.");
                        continue;
                    }

                    if (!Int32.TryParse(destinationEndpointIdentifierParts[0], out var slotId)) 
                    {
                        engine.Log($"Destination endpoint identifier '{destinationEndpoint.Identifier}' does not start with a valid slot id.");
                        continue;
                    }

                    // retrieve existing items
                    var getItemsRequest = new GenericRequest
                    {
                        Slot = slotId,
                        RequestType = GenericRequest.ReqType.IpGatewayOutputs,
                        RequestVerb = GenericRequest.ReqVerb.Get,
                        RequestData = GetItemsRequestSerialized(slotId),
                    };

                    var getItemsResponse = appearElement.SendMessage(getItemsRequest);
                    if (!getItemsResponse.Success)
                    {
                        engine.Log($"Retrieving IpGatewayOutputs from appear element '{elementId}' failed: {getItemsResponse.ResponseMessage}.");
                        continue;
                    }

                    if (!Guid.TryParse(destinationEndpointIdentifierParts[1], out var outputKey))
                    {
                        engine.Log($"Destination endpoint identifier '{destinationEndpoint.Identifier}' does not end with a valid output key.");
                        continue;
                    }

                    // select the item to edit
                    var apiResponse = SecureNewtonsoftDeserialization.DeserializeObject<GetOutputs.Response>(getItemsResponse.ResponseMessage)?.Result.Data;
                    var itemToEdit = apiResponse?.Find(x => x.Key == outputKey);
                    if (itemToEdit == null)
                    {
                        engine.Log($"No IpGatewayOutput with key '{outputKey}' found in appear element '{elementId}'.");
                        continue;
                    }

                    // update service to be received by output


                    var request = new GenericRequest
                    {
                        Slot = slotId,
                        RequestType = GenericRequest.ReqType.IpGatewayOutputs, // providing the type will automatically refresh the linked tables
                        RequestVerb = GenericRequest.ReqVerb.Post, // Post to update the item
                        RequestData = SetItemsRequestSerialized(slotId, itemToEdit),
                    };
                    var response = appearElement.SendMessage(request);
                    if (response.Success)
                    {
                        
                    }
                    else
                    {
                        engine.Log($"Updating IpGatewayOutput with key '{outputKey}' failed: {response.ResponseMessage}.");
                    }
                }
            }
        }

        public override void Disconnect(IEngine engine, IConnectionHandlerEngine connectionEngine, DisconnectDestinationsRequest disconnectDestinationsRequest)
        {
            var groupedByDestinationElement = disconnectDestinationsRequest.Destinations.GroupBy(x => x.Element);
            foreach (var group in groupedByDestinationElement)
            {
                var elementId = group.Key.Value;
                var element = engine.FindElement(elementId.AgentId, elementId.ElementId);

                foreach (var destination in group)
                {
                   
                }
            }
        }

        private static string GetItemsRequestSerialized(int slot)
        {
            // Info: sessionId (Guid.NewGuid()) statement is used by the connector to log the request/responses on the HTTP Communications page - providing any GUID is sufficient
            // Info: the slot number is required to specify from which slot you wish to collect the items for
            // Info: the Options can be left empty.
            return JsonConvert.SerializeObject(new GetOutputs.Request(Guid.NewGuid(), slot.ToString("x"), String.Empty), SerializerSettings);
        }

        private static string SetItemsRequestSerialized(int slot, Map<Guid, Output> item)
        {
            // Info: sessionId (Guid.NewGuid()) statement is used by the connector to log the request/responses on the HTTP Communications page - providing any GUID is sufficient
            // Info: the slot number is required to specify from which slot you wish to collect the items for
            // Info: the query is a list of items to update (1 or multiple possible!).
            return JsonConvert.SerializeObject(
                new SetOutputs.Request(Guid.NewGuid(), slot.ToString("x"), new SetQuery<Map<Guid, Output>>(new List<Map<Guid, Output>> { item })),
                SerializerSettings);
        }
    }
}
