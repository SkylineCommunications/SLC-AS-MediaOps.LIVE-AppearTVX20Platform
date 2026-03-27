namespace Skyline.DataMiner.ConnectorAPI.Appear.X
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Calls;
	using Skyline.DataMiner.ConnectorAPI.Appear.X.InterApp.Executors;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
	using Skyline.DataMiner.Core.InterAppCalls.Common.MessageExecution;
	using Skyline.DataMiner.Core.InterAppCalls.Common.Shared;
	using Skyline.DataMiner.Net;

	/// <summary>
	///     Represents an Appear X Element that allows to interface with it's API through Inter App messages.
	/// </summary>
	public class AppearXElement
	{
		/// <summary>
		///     ID of the parameter that's used to receive incoming InterApp messages.
		/// </summary>
		public const int InterAppReceive_ParameterId = 9000000;

		/// <summary>
		///     ID of the parameter that's used to return outgoing InterApp messages.
		/// </summary>
		public const int InterAppReturn_ParameterId = 9000001;

		private readonly IConnection connection;
		private readonly IDmsElement element;
		private TimeSpan timeout = TimeSpan.FromSeconds(10);

		/// <summary>
		///     Initializes a new instance of the <see cref="AppearXElement" /> class.
		/// </summary>
		/// <param name="connection">Connection used to communicate with an Appear X Element.</param>
		/// <param name="agentId">Agent ID of the Appear X Element.</param>
		/// <param name="elementId">Element ID of the Appear X Element.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided connection is not initialized.</exception>
		/// <exception cref="ElementStoppedException">Thrown when the provided element is not active.</exception>
		public AppearXElement(IConnection connection, int agentId, int elementId)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

			var dms = connection.GetDms();
			element = dms.GetElement(new DmsElementId(agentId, elementId));

			if (element.State != ElementState.Active)
			{
				throw new ElementStoppedException($"Element {element.Name} is not active");
			}
		}

		/// <summary>
		///     List of which executor needs to handle which type of message.<br />
		///     Required under the hood to correctly handle the communication.
		/// </summary>
		public static Dictionary<Type, Type> ExecutorMapping { get; } = new Dictionary<Type, Type>
		{
			{ typeof(ExpectedReturn), typeof(ExpectedReturnExecutor) },
		};

		/// <summary>
		///     List of Known InterApp Messages (Types) to be used during InterApp communication.<br />
		///     Required under the hood to correctly map the executors and types of messages.
		/// </summary>
		public static List<Type> KnownTypes { get; } = new List<Type>
		{
			typeof(GenericRequest),
			typeof(PlainRequest),
			typeof(ExpectedReturn),
			typeof(Source),
			typeof(ReturnAddress),
			typeof(Uri),
		};

		/// <summary>
		///     Maximum amount of time in which every request to the chassis should be handled.<br />
		///     Default: 10 seconds.<br />
		///     Maximum: 2 minutes.<br />
		/// </summary>
		public TimeSpan Timeout
		{
			get => timeout;
			set => timeout = value <= TimeSpan.FromSeconds(120) ? value : TimeSpan.FromSeconds(120);
		}

		/// <summary>
		///     Send the InterApp message to this Appear X Element.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="logger">Optional logger functionality.</param>
		/// <returns></returns>
		public InterAppResponse SendMessage(Message message, Action<string> logger = null)
		{
			try
			{
				IInterAppCall commands = InterAppCallFactory.CreateNew();
				commands.Messages.Add(message);
				commands.Source = new Source("AppearX_Element");
				commands.ReturnAddress = new ReturnAddress(element.AgentId, element.Id, InterAppReturn_ParameterId);

				Message returnedMessage = commands.Send(connection, element.AgentId, element.Id, InterAppReceive_ParameterId, timeout, KnownTypes, false).FirstOrDefault();
				if (returnedMessage == null)
				{
					logger?.Invoke($"{element.Name}|{nameof(SendMessage)}|Received response was null...");
					return new InterAppResponse { Success = false, ResponseMessage = "No InterApp response received." };
				}

				IMessageExecutor executor = returnedMessage.CreateExecutor(ExecutorMapping);
				bool status = executor.Validate();

				logger?.Invoke(
					status
						? $"{element.Name}|Response status: OK, message: {executor}"
						: $"{element.Name}|Response status: FAIL, message: {executor}");

				return new InterAppResponse { Success = status, ResponseMessage = executor.ToString() };
			}
			catch (TimeoutException)
			{
				logger?.Invoke($"{element.Name}|{nameof(SendMessage)}|Message timed out...");
				return new InterAppResponse { Success = false, ResponseMessage = "Message Timed out..." };
			}
			catch (Exception e)
			{
				logger?.Invoke($"{element.Name}|{nameof(SendMessage)}|Exception: {e}");
				return new InterAppResponse { Success = false, ResponseMessage = e.ToString() };
			}
		}
	}
}