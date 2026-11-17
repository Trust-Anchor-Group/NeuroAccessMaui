using System.Xml;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// The steps of a token's state machine and how it moves between them, read from the token's definition.
	/// </summary>
	/// <param name="StartState">The step the flow starts in, if given.</param>
	/// <param name="States">The steps, starting with the first and in the order they can be reached.</param>
	public sealed class TokenFlow(string? StartState, IReadOnlyList<TokenFlowState> States)
	{
		/// <summary>
		/// The step the flow starts in, if given.
		/// </summary>
		public string? StartState { get; } = StartState;

		/// <summary>
		/// The steps, starting with the first and in the order they can be reached.
		/// </summary>
		public IReadOnlyList<TokenFlowState> States { get; } = States;

		/// <summary>
		/// Reads the flow from a token's definition.
		/// </summary>
		/// <param name="DefinitionXml">The definition, which holds the state machine.</param>
		/// <returns>The flow, or null when the definition has no state machine with steps.</returns>
		public static TokenFlow? Parse(string? DefinitionXml)
		{
			if (string.IsNullOrWhiteSpace(DefinitionXml))
				return null;

			XmlDocument Doc = new()
			{
				XmlResolver = null,
				PreserveWhitespace = false
			};

			try
			{
				using StringReader Reader = new(DefinitionXml);
				using XmlReader Xml = XmlReader.Create(Reader, new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null
				});

				Doc.Load(Xml);
			}
			catch (XmlException)
			{
				return null;
			}

			XmlElement? Machine = Doc.DocumentElement;
			if (Machine is null)
				return null;

			if (Machine.LocalName != "StateMachine")
				Machine = FindDescendant(Machine, "StateMachine");

			if (Machine is null)
				return null;

			Dictionary<string, TokenFlowState> ById = new(StringComparer.Ordinal);
			List<string> DocumentOrder = [];

			foreach (XmlNode Node in Machine.ChildNodes)
			{
				if (Node is not XmlElement State || State.LocalName != "State")
					continue;

				string Id = State.GetAttribute("id");
				if (string.IsNullOrEmpty(Id) || ById.ContainsKey(Id))
					continue;

				ById[Id] = new TokenFlowState(Id, ReadExits(State));
				DocumentOrder.Add(Id);
			}

			if (ById.Count == 0)
				return null;

			string Start = Machine.GetAttribute("startState");
			return new TokenFlow(string.IsNullOrEmpty(Start) ? null : Start, OrderStates(Start, ById, DocumentOrder));
		}

		/// <summary>
		/// Reads the ways out of a step: its events that lead to another step.
		/// </summary>
		/// <param name="State">The step's element.</param>
		/// <returns>The ways out.</returns>
		private static List<TokenFlowExit> ReadExits(XmlElement State)
		{
			List<TokenFlowExit> Exits = [];

			foreach (XmlNode Node in State.ChildNodes)
			{
				if (Node is not XmlElement Event || Event.LocalName != "OnEvent")
					continue;

				string NewState = Event.GetAttribute("newState");
				string FailureState = Event.GetAttribute("failureState");

				if (string.IsNullOrEmpty(NewState) && string.IsNullOrEmpty(FailureState))
					continue;   // An event that only runs an action, and stays in the step.

				(TokenFlowTrigger Trigger, string? Argument) = ReadTrigger(Event);

				if (!string.IsNullOrEmpty(NewState) && NewState != State.GetAttribute("id"))
					Exits.Add(new TokenFlowExit(NewState, false, Trigger, Argument));

				if (!string.IsNullOrEmpty(FailureState) && FailureState != State.GetAttribute("id"))
					Exits.Add(new TokenFlowExit(FailureState, true, Trigger, Argument));
			}

			return Exits;
		}

		/// <summary>
		/// Reads what triggers an event.
		/// </summary>
		/// <param name="Event">The event's element.</param>
		/// <returns>The trigger, and more about it when given.</returns>
		private static (TokenFlowTrigger, string?) ReadTrigger(XmlElement Event)
		{
			foreach (XmlNode Node in Event.ChildNodes)
			{
				if (Node is not XmlElement Trigger)
					continue;

				string Text = Trigger.InnerText.Trim();

				return Trigger.LocalName switch
				{
					"OnCondition" => (TokenFlowTrigger.Condition, null),
					"OnDateTime" or "OnTime" => (TokenFlowTrigger.DateTime, Text),
					"OnDuration" => (TokenFlowTrigger.Duration, Text),
					"OnTransferred" => (TokenFlowTrigger.Transferred, null),
					"OnPaymentReceived" or "OnPayment" => (TokenFlowTrigger.PaymentReceived, null),
					"OnPaymentSent" => (TokenFlowTrigger.PaymentSent, null),
					"OnXmlNote" or "OnTextNote" or "OnNote" => (TokenFlowTrigger.OwnerNote, NoteName(Trigger)),
					"OnExternalXmlNote" or "OnExternalTextNote" or "OnExternalNote" => (TokenFlowTrigger.ExternalNote, NoteName(Trigger)),
					"OnContractSignature" or "OnContractSigned" => (TokenFlowTrigger.ContractSigned, null),
					"OnDestroyed" => (TokenFlowTrigger.Destroyed, null),
					"OnContractTemplateApproved" => (TokenFlowTrigger.TemplateApproved, NoteName(Trigger)),
					"OnEntryAdded" => (TokenFlowTrigger.EntryAdded, null),
					"OnEntryUpdated" => (TokenFlowTrigger.EntryUpdated, null),
					"OnEntryDeleted" => (TokenFlowTrigger.EntryDeleted, null),
					_ => (TokenFlowTrigger.Other, null)
				};
			}

			return (TokenFlowTrigger.Other, null);
		}

		/// <summary>
		/// Gets the name an event gives in its local name: the note it waits for, or the contract whose template is
		/// approved. Null when it names none.
		/// </summary>
		private static string? NoteName(XmlElement Trigger)
		{
			string LocalName = Trigger.GetAttribute("localName");
			return string.IsNullOrEmpty(LocalName) ? null : LocalName;
		}

		/// <summary>
		/// Orders the steps: first the start step, then the steps in the order they can be reached from it, and last any
		/// steps that can't be reached, in the order they are written.
		/// </summary>
		private static List<TokenFlowState> OrderStates(string Start, Dictionary<string, TokenFlowState> ById,
			List<string> DocumentOrder)
		{
			List<TokenFlowState> Result = [];
			HashSet<string> Added = new(StringComparer.Ordinal);
			Queue<string> Queue = new();

			Queue.Enqueue(!string.IsNullOrEmpty(Start) && ById.ContainsKey(Start) ? Start : DocumentOrder[0]);

			while (Queue.Count > 0)
			{
				string Id = Queue.Dequeue();
				if (!Added.Add(Id) || !ById.TryGetValue(Id, out TokenFlowState? State))
					continue;

				Result.Add(State);

				foreach (TokenFlowExit Exit in State.Exits.Where(Candidate => !Candidate.IsFailure))
					Queue.Enqueue(Exit.Target);

				foreach (TokenFlowExit Exit in State.Exits.Where(Candidate => Candidate.IsFailure))
					Queue.Enqueue(Exit.Target);
			}

			foreach (string Id in DocumentOrder)
			{
				if (Added.Add(Id))
					Result.Add(ById[Id]);
			}

			return Result;
		}

		/// <summary>
		/// Finds the first element with a given local name below an element.
		/// </summary>
		private static XmlElement? FindDescendant(XmlElement Parent, string LocalName)
		{
			foreach (XmlNode Node in Parent.ChildNodes)
			{
				if (Node is not XmlElement Child)
					continue;

				if (Child.LocalName == LocalName)
					return Child;

				XmlElement? Found = FindDescendant(Child, LocalName);
				if (Found is not null)
					return Found;
			}

			return null;
		}
	}
}
