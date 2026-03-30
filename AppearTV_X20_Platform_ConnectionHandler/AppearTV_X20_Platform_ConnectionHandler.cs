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
    using Skyline.DataMiner.Core.DataMinerSystem.Automation;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.DataSources.Appear.X.Schema;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.IpGateway.V1_31.Output.Command;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.Types.ComponentMapping.V1_2.Type;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.Types.GenericDescriptor.V1_1.Type;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.Types.Output.V1_29.Type;
    using Skyline.DataMiner.DataSources.Appear.X.Schema.Types.OutputMapping.V1_2.Type;
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
                    var destinationIdentifier = Convert.ToString(updatedRow[1]); // key
                    var inputSlot = Convert.ToInt32(updatedRow[12]); // input slot number
                    var sourceKey = Convert.ToString(updatedRow[20]); // source key
                    var sourceIdentifier = $"{inputSlot}.{sourceKey}";

                    var endpoints = connectionEngine.Api.Endpoints.GetByElementAndIdentifiers(update.DmsElementId, new[] { destinationIdentifier, sourceIdentifier });
                    
                    var destinationEndpoint = endpoints.FirstOrDefault(e => e.Identifier == destinationIdentifier);
                    if (destinationEndpoint == null)
                    {
                        continue;
                    }

                    var sourceEndpoint = endpoints.FirstOrDefault(e => e.Identifier == sourceIdentifier);
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

                    // check if row update wasn't received in same subscription update
                    if (!updatedConnections.Any(c => c.DestinationEndpoint.Equals(destinationEndpoint)))
                    {
                        // disconnect destination
                        updatedConnections.Add(new ConnectionUpdate(destinationEndpoint, isConnected: false));
                    }
                }
            }

            if (updatedConnections.Count > 0)
            {
                connectionEngine.RegisterConnections(updatedConnections);
            }
        }

        public override void Connect(IEngine engine, IConnectionHandlerEngine connectionEngine, CreateConnectionsRequest createConnectionsRequest)
        {
            var dms = engine.GetDms();

            var groupedByDestinationElement = createConnectionsRequest.Connections.GroupBy(x => x.DestinationEndpoint.Element);
            foreach (var group in groupedByDestinationElement)
            {
                var element = dms.GetElement(group.Key.Value);
                var appearElement = new AppearXElement(Engine.SLNetRaw, element.AgentId, element.Id);

                foreach (var connection in group)
                {
                    var sourceEndpoint = connection.SourceEndpoint;
                    if (!TryParseEndpointIdentifier(sourceEndpoint.Identifier, out var inputSlotId, out var inputKey))
                    {
                        engine.Log($"Source endpoint identifier '{sourceEndpoint.Identifier}' is not in correct format. It should be '[slot id].[input key]'.");
                        continue;
                    }

                    if (!TryGetSourceFlowId(element, inputKey, out var sourceFlowKey))
                    {
                        engine.Log($"Could not retrieve source flow from input with key '{inputKey}' in slot '{inputSlotId}'.");
                        continue;
                    }

                    var destinationEndpoint = connection.DestinationEndpoint;
                    if (!TryParseEndpointIdentifier(destinationEndpoint.Identifier, out var outputSlotId, out var outputKey))
                    {
                        engine.Log($"Destination endpoint identifier '{destinationEndpoint.Identifier}' is not in correct format. It should be '[slot id].[output key]'.");
                        continue;
                    }

                    if (!TryGetIpGatewayOutput(appearElement, outputSlotId, outputKey, out var output))
                    {
                        engine.Log($"Could not retrieve IpGatewayOutput with key '{outputKey}' in slot '{outputSlotId}'.");
                        continue;
                    }

                    try
                    {
                        UpdateIpGatewayOutputWithNewSourceId(appearElement, outputSlotId, output, sourceFlowKey);
                    }
                    catch (Exception e)
                    {
                        engine.Log($"Updating IpGatewayOutput with key '{outputKey}' in slot '{outputSlotId}' failed: {e}.");
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
                    // TODO: retrieve output and either disable output or remove service
                }
            }
        }

        private static bool TryParseEndpointIdentifier(string identifier, out int slotId, out Guid key)
        {
            slotId = 0;
            key = Guid.Empty;

            var identifierParts = identifier.Split('.');
            if (identifierParts.Length != 2)
            {
                return false;
            }

            if (!Int32.TryParse(identifierParts[0], out slotId))
            {
                return false;
            }

            if (!Guid.TryParse(identifierParts[1], out key))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetSourceFlowId(IDmsElement element, Guid key, out Guid sourceFlowKey)
        {
            sourceFlowKey = Guid.Empty;

            var inputServicesKey = element.GetTable(1400).GetPrimaryKeys().FirstOrDefault(x => x.StartsWith(key + "."));
            if (inputServicesKey == null)
            {
                return false;
            }

            if (!Guid.TryParse(inputServicesKey.Split('.').Last(), out sourceFlowKey))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetIpGatewayOutput(AppearXElement element, int slotId, Guid key, out Map<Guid, Output> output)
        {
            output = null;

            var request = new GenericRequest
            {
                Slot = slotId,
                RequestType = GenericRequest.ReqType.IpGatewayOutputs,
                RequestVerb = GenericRequest.ReqVerb.Get,
                RequestData = GetItemsRequestSerialized(slotId),
            };

            var response = element.SendMessage(request);
            if (!response.Success)
            {
                return false;
            }

            var apiResponse = SecureNewtonsoftDeserialization.DeserializeObject<GetOutputs.Response>(response.ResponseMessage)?.Result.Data;

            output = apiResponse?.Find(x => x.Key == key);
            return output != null;
        }

        private static void UpdateIpGatewayOutputWithNewSourceId(AppearXElement element, int slotId, Map<Guid, Output> output, Guid sourceFlowId)
        {
            if (output.Value.OutputSettings.TsWhitelistMode?.DvbMode != null)
            {
                var firstSourceMultiplex = output.Value.OutputSettings.TsWhitelistMode.DvbMode.Source.Multiplex.FirstOrDefault(x => x.Service != null);
                if (firstSourceMultiplex != null)
                {
                    firstSourceMultiplex.Service.Source = sourceFlowId;
                }
                else
                {
                    output.Value.OutputSettings.TsWhitelistMode.DvbMode.Source.Multiplex.Add(
                        new DvbMultiplexSource(
                            new DvbServiceSource
                            {
                                Source = sourceFlowId,
                                Priority = 100,
                                Settings = new DvbMultiplexServiceSettings
                                {
                                    ServiceId = new OptionalStruct<int>(),
                                    ServiceName = new OptionalClass<string>(),
                                    ServiceType = new OptionalStruct<int>(),
                                    ServiceProvider = new OptionalClass<string>(),
                                    PmtPid = new OptionalStruct<int>(),
                                    ComponentLayout = new ComponentLayoutMap
                                    {
                                        DefaultAction = ComponentMapDefaultAction.Options.PASSTHROUGH,
                                        Layout = new List<ComponentMapping>(),
                                    },
                                    ComponentGeneration = new List<ComponentGeneration>(),
                                    Descriptors = new List<GenericPmtDescriptorEntry>(),
                                    ScramblingIndex = new OptionalStruct<int>(),
                                },
                                InputRedundancy = new InputRedundancy
                                {
                                    Mode = InputRedundancySwitchMode.Options.Disabled,
                                    Backups = new List<Guid>(),
                                    PreferredSource = new OptionalClass<PreferredSource>(),
                                    SwitchbackDelay = 10,
                                    HotStandby = new OptionalClass<HotStandby>(),
                                },
                            }));
                }
            }
            else if (output.Value.OutputSettings.TsWhitelistMode?.MpegMode != null)
            {
                if (!output.Value.OutputSettings.TsWhitelistMode.MpegMode.Source.Multiplex.Any(x => x.Program != null))
                {
                    throw new NotSupportedException($"Output {output.Value.Label} contains no existing services to replace!");
                }

                output.Value.OutputSettings.TsWhitelistMode.MpegMode.Source.Multiplex.First(x => x.Program != null).Program.Source = sourceFlowId;
            }
            else if (output.Value.OutputSettings.TsWhitelistMode?.AtscMode != null)
            {
                if (!output.Value.OutputSettings.TsWhitelistMode.AtscMode.Source.Multiplex.Any(x => x.Channel != null))
                {
                    throw new NotSupportedException($"Output {output.Value.Label} contains no existing services to replace!");
                }

                output.Value.OutputSettings.TsWhitelistMode.AtscMode.Source.Multiplex.First(x => x.Channel != null).Channel.Source = sourceFlowId;
            }
            else if (output.Value.OutputSettings.RawMode != null)
            {
                output.Value.OutputSettings.RawMode.Source = sourceFlowId;
            }
            else if (output.Value.OutputSettings.TsBlacklistMode != null)
            {
                output.Value.OutputSettings.TsBlacklistMode.Source = sourceFlowId;
            }
            else
            {
                throw new NotSupportedException($"Output '{output.Value.Label}' contains an output mode which is not supported by this action.");
            }

            var request = new GenericRequest
            {
                Slot = slotId,
                RequestType = GenericRequest.ReqType.IpGatewayOutputs, // providing the type will automatically refresh the linked tables
                RequestVerb = GenericRequest.ReqVerb.Post, // Post to update the item
                RequestData = SetItemsRequestSerialized(slotId, output),
            };

            var response = element.SendMessage(request);
            if (!response.Success)
            {
                throw new InvalidOperationException($"Failed to update output '{output.Value.Label}' with new source flow: {response.ResponseMessage}");
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
