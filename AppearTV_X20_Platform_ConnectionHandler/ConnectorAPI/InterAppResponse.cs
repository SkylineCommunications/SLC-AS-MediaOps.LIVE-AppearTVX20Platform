namespace Skyline.DataMiner.ConnectorAPI.Appear.X
{
    /// <summary>
    ///     Response data from an executed InterApp Request.
    /// </summary>
    public sealed class InterAppResponse
    {
        /// <summary>
        ///     Status indicating if the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Return message from the device if all was OK<br />
        ///     OR Error message if something went wrong.
        /// </summary>
        public string ResponseMessage { get; set; }
    }
}