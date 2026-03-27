namespace Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls
{
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    /// <summary>
    ///     Generic Request InterApp message that can be re-used for multiple types.
    ///     E.G. Can be used to Retrieve IP Inputs -> ReqVerb.Get, Type.Inputs.
    ///     E.G. Can be used to Create IP Inputs -> ReqVerb.Post, Type.Inputs.
    /// </summary>
    public class GenericRequest : Message
    {
        /// <summary>
        ///     Type of request to add/update/delete.
        /// </summary>
        public enum ReqType
        {
            /// <summary>IP Gateway Inputs (IP/SRT/ZIXI).</summary>
            IpGatewayInputs,

            /// <summary>IP Gateway Outputs (IP/SRT/ZIXI).</summary>
            IpGatewayOutputs,

            /// <summary>SDI Encoder.</summary>
            XgerSdiEncoder,

            /// <summary>SDI Decoder.</summary>
            XgerSdiDecoder,

            /// <summary>SDI Transcoder.</summary>
            XgerSdiTranscoder,

            /// <summary>IP 2022-6 Encoder.</summary>
            XgerIp2022Encoder,

            /// <summary>IP 2022-6 Decoder.</summary>
            XgerIp2022Decoder,

            /// <summary>IP 2110 Encoder.</summary>
            XgerIp2110Encoder,

            /// <summary>IP 2110 Decoder.</summary>
            XgerIp2110Decoder,

            /// <summary>ASI.</summary>
            Asi,

            /// <summary>SDI.</summary>
            Sdi,

            /// <summary>S2X Input (Demodulator).</summary>
            S2xIn,

            /// <summary>S2X Output (Modulator).</summary>
            S2xOut,

            /// <summary>Scrambler.</summary>
            Scrambler,

            /// <summary>Bulk Descrambler.</summary>
            Descrambler,
        }

        /// <summary>
        ///     Request verb (GET/POST/DELETE)
        /// </summary>
        public enum ReqVerb
        {
            ///<summary>GET Request Verb.</summary>
            Get,

            ///<summary>Post Request Verb.</summary>
            Post,

            ///<summary>Delete Request Verb.</summary>
            Delete,
        }

        /// <summary>
        ///     Request verb used by the command.
        /// </summary>
        public ReqVerb RequestVerb { get; set; }

        /// <summary>
        ///     Request type used by the command (will determine which type is retrieved/updated/deleted and refreshed.
        /// </summary>
        public ReqType RequestType { get; set; }

        /// <summary>
        ///     Gets or sets the slot number in case you wish to collect data from a certain slot.
        /// </summary>
        public int? Slot { get; set; }

        /// <summary>
        ///     Gets or sets the serialized JSON string of your item.
        ///     Only required when RequestVerb is of type POST.
        /// </summary>
        public string RequestData { get; set; }
    }
}