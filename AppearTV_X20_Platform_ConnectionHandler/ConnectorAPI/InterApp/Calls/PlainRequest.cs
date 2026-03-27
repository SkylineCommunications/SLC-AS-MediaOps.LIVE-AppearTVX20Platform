namespace Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls
{
    using System;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    /// <summary>
    ///     Plain Request InterApp message that can be re-used for all types.
    ///     E.G. Can be used to Retrieve IP Inputs -> ReqVerb.Get + provide endpoint url (/ui/1/board/1/api/jsonrpc).
    ///     E.G. Can be used to Create IP Inputs -> ReqVerb.Post + provide endpoint url.
    /// </summary>
    public class PlainRequest : Message
    {
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
        ///     Request Verb used by the command.
        /// </summary>
        public ReqVerb RequestVerb { get; set; }

        /// <summary>
        ///     Gets or sets the endpoint URL to which endpoint you wish to send the command to (E.G. "/ui/1/board/1/api/jsonrpc").
        /// </summary>
        public Uri EndPoint { get; set; }

        /// <summary>
        ///     Gets or sets the serialized JSON string of your item.
        ///     Only required when RequestVerb is of type POST.
        /// </summary>
        public string RequestData { get; set; }
    }
}