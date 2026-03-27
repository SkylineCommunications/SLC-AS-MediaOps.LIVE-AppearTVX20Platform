namespace Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls
{
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    /// <summary>
    ///     Inter App Response object, this is the data that will be sent back on a return message to indicate if all was OK or
    ///     possible error messages.
    /// </summary>
    public class ExpectedReturn : Message
    {
        /// <summary>
        ///     Gets or sets the status if the request was parsed and received OK.
        ///     E.G Device returned a 200 OK response for the request.
        /// </summary>
        public bool SuccessStatus { get; set; }

        /// <summary>
        ///     Gets or sets the additional status information about the response.
        ///     E.G. On an error response from the device will contain something like: {"error": "Could not update due to..." }.
        /// </summary>
        public string StatusMessage { get; set; }
    }
}