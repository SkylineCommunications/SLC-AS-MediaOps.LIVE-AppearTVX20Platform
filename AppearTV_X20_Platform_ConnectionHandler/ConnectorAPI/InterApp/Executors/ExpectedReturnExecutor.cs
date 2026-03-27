namespace Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Executors
{
    using Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Core.InterAppCalls.Common.MessageExecution;

    /// <summary>
    ///     InterApp Executor for the <see cref="ExpectedReturn" /> response messages.
    /// </summary>
    public class ExpectedReturnExecutor : MessageExecutor<ExpectedReturn>
    {
        /// <inheritdoc />
        public ExpectedReturnExecutor(ExpectedReturn message) : base(message)
        {
        }

        /// <inheritdoc />
        public override Message CreateReturnMessage()
        {
            return null; // not required
        }

        /// <inheritdoc />
        public override void DataGets(object dataSource)
        {
        }

        /// <inheritdoc />
        public override void DataSets(object dataDestination)
        {
        }

        /// <inheritdoc />
        public override void Modify()
        {
        }

        /// <inheritdoc />
        public override void Parse()
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Message.StatusMessage;
        }

        /// <inheritdoc />
        public override bool Validate()
        {
            return Message.SuccessStatus;
        }
    }
}