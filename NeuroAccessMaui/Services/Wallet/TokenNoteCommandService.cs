using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Xmpp;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using NeuroFeatures.Events;
using NeuroFeatures.NoteCommands;
using System.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Script;
using Waher.Script.Functions.Runtime;
using Waher.Script.Model;
using Waher.Script.Operators;
using Waher.Script.Operators.Assignments;
using Waher.Script.Operators.Membership;

namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Discovers and safely executes state-machine actions that generate token updates.
	/// </summary>
	[Singleton]
	internal sealed class TokenNoteCommandService : ITokenNoteCommandService
	{
		private static readonly HashSet<string> prohibitedScriptAssemblies =
		[
			"Waher.Script.Content",
			"Waher.Script.Data",
			"Waher.Script.Fractals",
			"Waher.Script.Networking",
			"Waher.Script.Persistence"
		];

		private readonly IXmppService xmppService;
		private readonly ITagProfile tagProfile;
		private readonly ILogService logService;

		/// <summary>
		/// Initializes the token-defined action service.
		/// </summary>
		/// <param name="XmppService">XMPP service used for live state and submission.</param>
		/// <param name="TagProfile">Current account and approved identity profile.</param>
		/// <param name="LogService">Redacted diagnostic logger.</param>
		public TokenNoteCommandService(
			IXmppService XmppService,
			ITagProfile TagProfile,
			ILogService LogService)
		{
			this.xmppService = XmppService;
			this.tagProfile = TagProfile;
			this.logService = LogService;
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyList<TokenNoteCommandDescriptor>> DiscoverAsync(
			Token Token,
			string Language,
			CancellationToken CancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(Token);
			CancellationToken.ThrowIfCancellationRequested();

			if (!Token.HasStateMachine)
				return [];

			NoteCommand[] Commands = await Token.GetNoteCommands();
			if (Commands.Length == 0)
				return [];

			CommandContext Context = await this.LoadContextAsync(Token, CancellationToken);
			List<TokenNoteCommandDescriptor> Result = [];

			for (int i = 0; i < Commands.Length; i++)
			{
				CancellationToken.ThrowIfCancellationRequested();
				NoteCommand Command = Commands[i];

				try
				{
					if (!IsRoleEligible(Command, Context.IsOwner) ||
						!HasSupportedParameterSchema(Command.Parameters))
					{
						continue;
					}

					if (!await IsContextEligibleAsync(Command, Context.Variables))
						continue;

					// Generation and parameter expressions are checked during discovery so unsafe actions never
					// become executable UI. The same checks are repeated immediately before execution.
					GetSafeExpression(Command.NoteGenerationScript);
					ValidateParameterExpressionSafety(Command.Parameters);

					string Title = FindLocalizedText(Command.Title, Language);
					if (string.IsNullOrWhiteSpace(Title))
						Title = string.IsNullOrWhiteSpace(Command.Id)
							? ServiceRef.Localizer[nameof(AppResources.TokenDefinedActionFallback)]
							: Command.Id;

					Result.Add(new TokenNoteCommandDescriptor(
						i,
						Command.Id,
						Title,
						FindLocalizedText(Command.ToolTip, Language),
						FindLocalizedText(Command.Confirmation, Language),
						FindLocalizedText(Command.Success, Language),
						FindLocalizedText(Command.Failure, Language),
						Command.Personal,
						Context.IsOwner,
						Context.CurrentState,
						Command.Parameters ?? []));
				}
				catch (UnsafeCommandException)
				{
					this.logService.LogWarning(
						"Token-defined action was excluded by the script safety policy.");
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception Ex)
				{
					this.LogRedactedFailure("Token-defined action discovery failed.", Ex);
				}
			}

			return Result;
		}

		/// <inheritdoc/>
		public async Task<TokenNoteCommandResult> ValidateAsync(
			Token Token,
			TokenNoteCommandDescriptor Descriptor,
			IReadOnlyDictionary<string, object?> ParameterValues,
			string Language,
			CancellationToken CancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(Token);
			ArgumentNullException.ThrowIfNull(Descriptor);
			ArgumentNullException.ThrowIfNull(ParameterValues);

			try
			{
				PreparedCommand Prepared = await this.PrepareAsync(
					Token,
					Descriptor,
					ParameterValues,
					Language,
					CancellationToken);

				if (Prepared.ParameterErrors.Count > 0)
				{
					return new TokenNoteCommandResult(
						TokenNoteCommandStatus.ValidationFailed,
						Prepared.FailureText,
						Prepared.ParameterErrors);
				}

				return new TokenNoteCommandResult(TokenNoteCommandStatus.Ready, string.Empty);
			}
			catch (UnsafeCommandException)
			{
				this.logService.LogWarning(
					"Token-defined action validation was blocked by the script safety policy.");
				return new TokenNoteCommandResult(TokenNoteCommandStatus.Unsafe, string.Empty);
			}
			catch (UnavailableCommandException)
			{
				return new TokenNoteCommandResult(TokenNoteCommandStatus.NotAvailable, string.Empty);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				this.LogRedactedFailure("Token-defined action validation failed.", Ex);
				return new TokenNoteCommandResult(
					TokenNoteCommandStatus.Failed,
					Descriptor.FailureText);
			}
		}

		/// <inheritdoc/>
		public async Task<TokenNoteCommandResult> ExecuteAsync(
			Token Token,
			TokenNoteCommandDescriptor Descriptor,
			IReadOnlyDictionary<string, object?> ParameterValues,
			string Language,
			CancellationToken CancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(Token);
			ArgumentNullException.ThrowIfNull(Descriptor);
			ArgumentNullException.ThrowIfNull(ParameterValues);

			try
			{
				PreparedCommand Prepared = await this.PrepareAsync(
					Token,
					Descriptor,
					ParameterValues,
					Language,
					CancellationToken);

				if (Prepared.ParameterErrors.Count > 0)
				{
					return new TokenNoteCommandResult(
						TokenNoteCommandStatus.ValidationFailed,
						Prepared.FailureText,
						Prepared.ParameterErrors);
				}

				CancellationToken.ThrowIfCancellationRequested();
				object? Generated;
				try
				{
					Generated = await Prepared.GenerationExpression.EvaluateAsync(Prepared.Variables);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception Ex)
				{
					this.LogRedactedFailure("Token-defined action generation failed.", Ex);
					return new TokenNoteCommandResult(
						TokenNoteCommandStatus.Failed,
						Prepared.FailureText);
				}

				bool IsXml;
				string Content;
				// Remote generation is intentionally constrained to the protocol note types already
				// supported by the app. No arbitrary object serialization or dynamic dispatch is allowed.
				switch (Generated)
				{
					case string Text when !string.IsNullOrWhiteSpace(Text):
						IsXml = false;
						Content = Text;
						break;

					case XmlDocument Document when Document.DocumentElement is not null:
						IsXml = true;
						Content = Document.DocumentElement.OuterXml;
						break;

					case XmlElement Element:
						IsXml = true;
						Content = Element.OuterXml;
						break;

					default:
						return new TokenNoteCommandResult(
							TokenNoteCommandStatus.UnsupportedResult,
							Prepared.FailureText);
				}

				int MatchingEventsBefore = await this.TryCountMatchingEventsAsync(
					Token.TokenId,
					Content,
					IsXml,
					Prepared.Command.Personal);

				try
				{
					if (IsXml)
					{
						await this.xmppService.AddNeuroFeatureXmlNote(
							Token.TokenId,
							Content,
							Prepared.Command.Personal);
					}
					else
					{
						await this.xmppService.AddNeuroFeatureTextNote(
							Token.TokenId,
							Content,
							Prepared.Command.Personal);
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception Ex)
				{
					this.LogRedactedFailure("Token-defined action submission failed.", Ex);
					if (IsDefiniteSubmissionFailure(Ex))
					{
						return new TokenNoteCommandResult(
							TokenNoteCommandStatus.Failed,
							Prepared.FailureText);
					}

					if (MatchingEventsBefore >= 0)
					{
						int MatchingEventsAfter = await this.TryCountMatchingEventsAsync(
							Token.TokenId,
							Content,
							IsXml,
							Prepared.Command.Personal);
						if (MatchingEventsAfter > MatchingEventsBefore)
						{
							return new TokenNoteCommandResult(
								TokenNoteCommandStatus.Succeeded,
								Prepared.SuccessText);
						}
					}

					return new TokenNoteCommandResult(
						TokenNoteCommandStatus.OutcomeUncertain,
						Prepared.FailureText);
				}

				return new TokenNoteCommandResult(
					TokenNoteCommandStatus.Succeeded,
					Prepared.SuccessText);
			}
			catch (UnsafeCommandException)
			{
				this.logService.LogWarning(
					"Token-defined action execution was blocked by the script safety policy.");
				return new TokenNoteCommandResult(TokenNoteCommandStatus.Unsafe, string.Empty);
			}
			catch (UnavailableCommandException)
			{
				return new TokenNoteCommandResult(TokenNoteCommandStatus.NotAvailable, string.Empty);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				this.LogRedactedFailure("Token-defined action execution failed.", Ex);
				return new TokenNoteCommandResult(
					TokenNoteCommandStatus.Failed,
					Descriptor.FailureText);
			}
		}

		private async Task<PreparedCommand> PrepareAsync(
			Token Token,
			TokenNoteCommandDescriptor Descriptor,
			IReadOnlyDictionary<string, object?> ParameterValues,
			string Language,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (!Token.HasStateMachine)
				throw new UnavailableCommandException();

			NoteCommand[] Commands = await Token.GetNoteCommands();
			if (Descriptor.CommandIndex < 0 || Descriptor.CommandIndex >= Commands.Length)
				throw new UnavailableCommandException();

			NoteCommand Command = Commands[Descriptor.CommandIndex];
			if (!string.Equals(
				Command.Id?.Trim() ?? string.Empty,
				Descriptor.Id,
				StringComparison.Ordinal))
			{
				throw new UnavailableCommandException();
			}

			CommandContext Context = await this.LoadContextAsync(Token, CancellationToken);
			if (!IsRoleEligible(Command, Context.IsOwner) ||
				Context.IsOwner != Descriptor.IsOwnerContext ||
				!HasSupportedParameterSchema(Command.Parameters) ||
				!HasMatchingParameterSchema(Command.Parameters, Descriptor.Parameters) ||
				!await IsContextEligibleAsync(Command, Context.Variables))
			{
				throw new UnavailableCommandException();
			}

			Expression GenerationExpression = GetSafeExpression(Command.NoteGenerationScript);
			ValidateParameterExpressionSafety(Command.Parameters);

			Variables Variables = CopyVariables(Context.Variables);
			Dictionary<string, string> ParameterErrors =
				new(StringComparer.Ordinal);

			foreach (Parameter Parameter in Command.Parameters ?? [])
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (!ParameterValues.TryGetValue(Parameter.Name, out object? Value) ||
					Value is null)
				{
					ParameterErrors[Parameter.Name] = string.Empty;
					continue;
				}

				try
				{
					Parameter.SetValue(Value);
					Parameter.Populate(Variables);
				}
				catch (Exception)
				{
					ParameterErrors[Parameter.Name] = string.Empty;
				}
			}

			foreach (Parameter Parameter in Command.Parameters ?? [])
			{
				if (ParameterErrors.ContainsKey(Parameter.Name))
					continue;

				try
				{
					if (!await Parameter.IsParameterValid(
						Variables,
						this.xmppService.ContractsClient))
					{
						ParameterErrors[Parameter.Name] = string.Empty;
					}
				}
				catch (Exception)
				{
					ParameterErrors[Parameter.Name] = string.Empty;
				}
			}

			return new PreparedCommand(
				Command,
				Variables,
				GenerationExpression,
				FindLocalizedText(Command.Success, Language),
				FindLocalizedText(Command.Failure, Language),
				ParameterErrors);
		}

		private async Task<CommandContext> LoadContextAsync(
			Token Token,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			CurrentStateEventArgs State =
				await this.xmppService.GetNeuroFeatureCurrentState(Token.TokenId);
			CancellationToken.ThrowIfCancellationRequested();

			if (!State.Ok)
				throw new UnavailableCommandException();

			Variables Variables = new();
			State.Variables?.CopyTo(Variables);

			// These reserved values are assigned last so a machine variable cannot replace the authoritative state.
			Variables["State"] = State.CurrentState;
			Variables["<State>"] = State.CurrentState;

			return new CommandContext(
				IsCurrentOwner(Token, this.tagProfile, this.xmppService.BareJid),
				State.CurrentState ?? string.Empty,
				Variables);
		}

		private static bool IsCurrentOwner(
			Token Token,
			ITagProfile TagProfile,
			string CurrentBareJid)
		{
			Waher.Networking.XMPP.Contracts.LegalIdentity? Identity = TagProfile.LegalIdentity;
			if (Identity?.IsApproved() == true &&
				!string.IsNullOrWhiteSpace(Identity.Id) &&
				string.Equals(Identity.Id, Token.Owner, StringComparison.Ordinal))
			{
				return true;
			}

			string IdentityJid = Identity?.IsApproved() == true
				? Identity.GetJid()
				: string.Empty;
			string EffectiveJid = string.IsNullOrWhiteSpace(IdentityJid)
				? CurrentBareJid
				: IdentityJid;

			return !string.IsNullOrWhiteSpace(EffectiveJid) &&
				!string.IsNullOrWhiteSpace(Token.OwnerJid) &&
				string.Equals(
					ToBareJid(EffectiveJid),
					ToBareJid(Token.OwnerJid),
					StringComparison.OrdinalIgnoreCase);
		}

		private static string ToBareJid(string Jid)
		{
			string Trimmed = Jid?.Trim() ?? string.Empty;
			int Separator = Trimmed.IndexOf('/');
			return Separator < 0 ? Trimmed : Trimmed[..Separator];
		}

		private static bool IsRoleEligible(NoteCommand Command, bool IsOwner)
		{
			return IsOwner ? Command.OwnerNote : Command.ExternalNote;
		}

		private static async Task<bool> IsContextEligibleAsync(
			NoteCommand Command,
			Variables Variables)
		{
			if (string.IsNullOrWhiteSpace(Command.NoteContextScript))
				return true;

			Expression ContextExpression = GetSafeExpression(Command.NoteContextScript);
			object? Result = await ContextExpression.EvaluateAsync(CopyVariables(Variables));
			return Result is bool IsApplicable && IsApplicable;
		}

		private static Variables CopyVariables(Variables Source)
		{
			Variables Result = new();
			Source.CopyTo(Result);
			return Result;
		}

		private static bool HasSupportedParameterSchema(Parameter[]? Parameters)
		{
			HashSet<string> Names = new(StringComparer.Ordinal);
			foreach (Parameter Parameter in Parameters ?? [])
			{
				if (string.IsNullOrWhiteSpace(Parameter.Name) ||
					!Names.Add(Parameter.Name) ||
					Parameter is not (
						StringParameter or
						NumericalParameter or
						BooleanParameter or
						DateParameter or
						DateTimeParameter or
						TimeParameter or
						DurationParameter))
				{
					return false;
				}
			}

			return true;
		}

		private static bool HasMatchingParameterSchema(
			Parameter[]? Current,
			IReadOnlyList<Parameter> Discovered)
		{
			Parameter[] CurrentParameters = Current ?? [];
			if (CurrentParameters.Length != Discovered.Count)
				return false;

			for (int i = 0; i < CurrentParameters.Length; i++)
			{
				if (!string.Equals(
						CurrentParameters[i].Name,
						Discovered[i].Name,
						StringComparison.Ordinal) ||
					CurrentParameters[i].GetType() != Discovered[i].GetType())
				{
					return false;
				}
			}

			return true;
		}

		private static void ValidateParameterExpressionSafety(Parameter[]? Parameters)
		{
			foreach (Parameter Parameter in Parameters ?? [])
			{
				if (!string.IsNullOrWhiteSpace(Parameter.Expression))
					GetSafeExpression(Parameter.Expression);
			}
		}

		private static Expression GetSafeExpression(string Script)
		{
			if (string.IsNullOrWhiteSpace(Script))
				throw new UnsafeCommandException();

			Expression Expression;
			try
			{
				Expression = new Expression(Script);
			}
			catch (Exception)
			{
				throw new UnsafeCommandException();
			}

			ScriptNode? Prohibited = null;
			bool Safe = Expression.ForAll(
				(ScriptNode Node, out ScriptNode NewNode, object State) =>
				{
					NewNode = null!;
					if (IsProhibitedScriptNode(Node))
					{
						Prohibited = Node;
						return false;
					}

					return true;
				},
				null!,
				SearchMethod.TreeOrder);

			if (!Safe || Prohibited is not null)
				throw new UnsafeCommandException();

			return Expression;
		}

		private static bool IsProhibitedScriptNode(ScriptNode Node)
		{
			Type NodeType = Node.GetType();
			string AssemblyName = NodeType.Assembly.GetName().Name ?? string.Empty;
			if (prohibitedScriptAssemblies.Contains(AssemblyName))
			{
				bool IsAllowedContentValue =
					AssemblyName == "Waher.Script.Content" &&
					(NodeType.FullName == "Waher.Script.Content.Functions.Duration" ||
						NodeType.Namespace ==
							"Waher.Script.Content.Functions.Encoding");
				if (!IsAllowedContentValue)
					return true;
			}

			return Node is
				NamedMember or
				NamedMemberAssignment or
				LambdaDefinition or
				NamedMethodCall or
				DynamicFunctionCall or
				DynamicMember or
				Create or
				Destroy or
				Error;
		}

		private static string FindLocalizedText(
			LocalizedString[]? Texts,
			string Language)
		{
			if (Texts is null || Texts.Length == 0)
				return string.Empty;

			string Preferred = Language?.Trim() ?? string.Empty;
			string Neutral = Preferred;
			int HyphenIndex = Preferred.IndexOf('-');
			int UnderscoreIndex = Preferred.IndexOf('_');
			int Separator = HyphenIndex < 0
				? UnderscoreIndex
				: UnderscoreIndex < 0
					? HyphenIndex
					: Math.Min(HyphenIndex, UnderscoreIndex);
			if (Separator > 0)
				Neutral = Preferred[..Separator];

			LocalizedString? Match = Texts.FirstOrDefault(Text =>
				string.Equals(Text.Language, Preferred, StringComparison.OrdinalIgnoreCase));
			Match ??= Texts.FirstOrDefault(Text =>
				string.Equals(Text.Language, Neutral, StringComparison.OrdinalIgnoreCase));
			Match ??= Texts.FirstOrDefault(Text =>
				string.Equals(Text.Language, "en", StringComparison.OrdinalIgnoreCase));
			Match ??= Texts.FirstOrDefault(Text => !string.IsNullOrWhiteSpace(Text.Text));

			return Match?.Text?.Trim() ?? string.Empty;
		}

		private async Task<int> TryCountMatchingEventsAsync(
			string TokenId,
			string Content,
			bool IsXml,
			bool Personal)
		{
			try
			{
				TokenEvent[] Events = await this.xmppService.GetNeuroFeatureEvents(TokenId);
				return Events.Count(Event =>
					Event is TokenNoteEvent Note &&
					Event.Personal == Personal &&
					string.Equals(Note.Note, Content, StringComparison.Ordinal) &&
					(IsXml
						? Event is NoteXml or ExternalNoteXml
						: Event is NoteText or ExternalNoteText));
			}
			catch (Exception Ex)
			{
				this.LogRedactedFailure(
					"Token-defined action verification refresh failed.",
					Ex);
				return -1;
			}
		}

		private static bool IsDefiniteSubmissionFailure(Exception Exception)
		{
			Exception Current = Exception;
			while (Current is AggregateException && Current.InnerException is not null)
				Current = Current.InnerException;

			return Current is ArgumentException ||
				Current is XmppException XmppException &&
					XmppException.Stanza is not null;
		}

		private void LogRedactedFailure(string Message, Exception Exception)
		{
			// Script text, generated content, state variables, parameter values, and token IDs are deliberately omitted.
			this.logService.LogWarning(
				Message,
				new KeyValuePair<string, object?>(
					"FailureType",
					Exception.GetType().Name));
		}

		/// <summary>
		/// Holds the authoritative state and relationship context for one eligibility pass.
		/// </summary>
		private sealed class CommandContext
		{
			/// <summary>
			/// Initializes an eligibility context.
			/// </summary>
			/// <param name="IsOwner">Whether the current identity owns the token.</param>
			/// <param name="CurrentState">Authoritative current state.</param>
			/// <param name="Variables">Isolated state and current-variable context.</param>
			public CommandContext(
				bool IsOwner,
				string CurrentState,
				Variables Variables)
			{
				this.IsOwner = IsOwner;
				this.CurrentState = CurrentState;
				this.Variables = Variables;
			}

			/// <summary>
			/// Gets a value indicating whether the current identity owns the token.
			/// </summary>
			public bool IsOwner { get; }

			/// <summary>
			/// Gets the authoritative current state.
			/// </summary>
			public string CurrentState { get; }

			/// <summary>
			/// Gets an isolated state and current-variable context.
			/// </summary>
			public Variables Variables { get; }
		}

		/// <summary>
		/// Holds a freshly revalidated command immediately before generation or submission.
		/// </summary>
		private sealed class PreparedCommand
		{
			/// <summary>
			/// Initializes a prepared command.
			/// </summary>
			/// <param name="Command">Fresh protocol command.</param>
			/// <param name="Variables">Validated, isolated generation variables.</param>
			/// <param name="GenerationExpression">Safety-checked generation expression.</param>
			/// <param name="SuccessText">Localized command success copy.</param>
			/// <param name="FailureText">Localized command failure copy.</param>
			/// <param name="ParameterErrors">Invalid parameter names.</param>
			public PreparedCommand(
				NoteCommand Command,
				Variables Variables,
				Expression GenerationExpression,
				string SuccessText,
				string FailureText,
				IReadOnlyDictionary<string, string> ParameterErrors)
			{
				this.Command = Command;
				this.Variables = Variables;
				this.GenerationExpression = GenerationExpression;
				this.SuccessText = SuccessText;
				this.FailureText = FailureText;
				this.ParameterErrors = ParameterErrors;
			}

			/// <summary>
			/// Gets the fresh protocol command.
			/// </summary>
			public NoteCommand Command { get; }

			/// <summary>
			/// Gets validated, isolated generation variables.
			/// </summary>
			public Variables Variables { get; }

			/// <summary>
			/// Gets the safety-checked generation expression.
			/// </summary>
			public Expression GenerationExpression { get; }

			/// <summary>
			/// Gets localized command success copy.
			/// </summary>
			public string SuccessText { get; }

			/// <summary>
			/// Gets localized command failure copy.
			/// </summary>
			public string FailureText { get; }

			/// <summary>
			/// Gets invalid parameter names.
			/// </summary>
			public IReadOnlyDictionary<string, string> ParameterErrors { get; }
		}

		/// <summary>
		/// Signals that externally supplied command logic failed the safety policy.
		/// </summary>
		private sealed class UnsafeCommandException : Exception
		{
		}

		/// <summary>
		/// Signals that a command no longer matches the current token role, definition, or state.
		/// </summary>
		private sealed class UnavailableCommandException : Exception
		{
		}
	}
}
