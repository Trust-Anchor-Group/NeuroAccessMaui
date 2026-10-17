using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroFeatures.NoteCommands;
using Waher.Networking.XMPP.Contracts;
using Waher.Script;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Runs the note commands of a token's state machine: checks their scripts, generates the note from the parameter
	/// values, and adds it to the token.
	/// </summary>
	public static class NoteCommandRunner
	{
		/// <summary>
		/// How long the scripts of a command may run before they are stopped.
		/// </summary>
		public static readonly TimeSpan ScriptTimeout = TimeSpan.FromSeconds(5);

		// Names the current state has when a condition runs, as in LegalLab.
		private const string StateVariable = "State";
		private const string StateTagVariable = "<State>";

		// Commands whose condition isn't allowed, so the reason is logged once per command.
		private static readonly ConditionalWeakTable<NoteCommand, object> refusedConditions = new();

		/// <summary>
		/// Picks the text for the user's language: the exact culture, then the same language, then English, then text
		/// without a language, and last the first text there is.
		/// </summary>
		/// <param name="Texts">The localized texts.</param>
		/// <param name="Culture">The user's culture.</param>
		/// <returns>The text, or null when there is none.</returns>
		public static string? SelectText(LocalizedString[]? Texts, CultureInfo Culture)
		{
			if (Texts is null || Texts.Length == 0)
				return null;

			string Language = Culture.TwoLetterISOLanguageName;

			LocalizedString Selected =
				Texts.FirstOrDefault(Text => string.Equals(Text.Language, Culture.Name, StringComparison.OrdinalIgnoreCase)) ??
				Texts.FirstOrDefault(Text => string.Equals(GetLanguage(Text.Language), Language, StringComparison.OrdinalIgnoreCase)) ??
				Texts.FirstOrDefault(Text => string.Equals(GetLanguage(Text.Language), "en", StringComparison.OrdinalIgnoreCase)) ??
				Texts.FirstOrDefault(Text => string.IsNullOrEmpty(Text.Language)) ??
				Texts[0];

			string? Result = Selected.Text?.Trim();
			return string.IsNullOrEmpty(Result) ? null : Result;
		}

		/// <summary>
		/// Picks the text for the current language.
		/// </summary>
		/// <param name="Texts">The localized texts.</param>
		/// <returns>The text, or null when there is none.</returns>
		public static string? SelectText(LocalizedString[]? Texts)
		{
			return SelectText(Texts, CultureInfo.CurrentUICulture);
		}

		/// <summary>
		/// Checks if the app may run a command: it has a note generation script, and that script and the parameters'
		/// validation scripts parse and pass <see cref="NoteScriptValidator"/>. The command's condition, its note context
		/// script, must parse. It is checked with the state machine's variables by <see cref="IsAvailableAsync"/>.
		/// </summary>
		/// <param name="Command">The note command.</param>
		/// <param name="AssignedNames">Names the scripts assign that aren't parameters.</param>
		/// <returns>If the command may run.</returns>
		public static bool CanRun(NoteCommand Command, out string[] AssignedNames)
		{
			AssignedNames = [];

			try
			{
				Expression? Generation = Command.ParsedNoteGenerationScript;
				if (Generation is null)
					return false;

				if (!string.IsNullOrWhiteSpace(Command.NoteContextScript) && Command.ParsedNoteContextScript is null)
					return false;

				List<Expression?> Scripts = [Generation];
				List<string> ParameterNames = [];

				foreach (Parameter Parameter in Command.Parameters ?? [])
				{
					if (string.IsNullOrEmpty(Parameter.Name))
						return false;

					ParameterNames.Add(Parameter.Name);

					if (!string.IsNullOrWhiteSpace(Parameter.Expression))
						Scripts.Add(Parameter.Parsed);     // Throws if the script doesn't parse.
				}

				return NoteScriptValidator.IsAllowed(Scripts, ParameterNames, out AssignedNames);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				AssignedNames = [];
				return false;
			}
		}

		/// <summary>
		/// Creates the variables the scripts of a command run with: the parameter values, and the names the scripts assign,
		/// set to null so they can't refer to anything else.
		/// </summary>
		/// <param name="Command">The note command.</param>
		/// <param name="AssignedNames">Names the scripts assign that aren't parameters.</param>
		/// <returns>A new set of variables.</returns>
		public static Variables CreateVariables(NoteCommand Command, string[] AssignedNames)
		{
			Variables Variables = new();

			foreach (string Name in AssignedNames)
				Variables[Name] = null;

			foreach (Parameter Parameter in Command.Parameters ?? [])
				Parameter.Populate(Variables);

			return Variables;
		}

		/// <summary>
		/// Checks the value of a parameter, using its validation script, which is stopped if it runs too long.
		/// </summary>
		/// <param name="Parameter">The parameter.</param>
		/// <param name="Variables">Variables holding the current parameter values.</param>
		/// <returns>If the value is valid.</returns>
		public static async Task<bool> IsParameterValidAsync(Parameter Parameter, Variables Variables)
		{
			ContractsClient? Client = null;

			try
			{
				Client = ServiceRef.XmppService.ContractsClient;
			}
			catch (Exception)
			{
				// Without a contracts client, only the checks that need no connection are made.
			}

			Task<bool> Validation = Task.Run(() => Parameter.IsParameterValid(Variables, Client));
			Task Completed = await Task.WhenAny(Validation, Task.Delay(ScriptTimeout));

			if (Completed != Validation)
			{
				Variables.Abort();
				return false;
			}

			return await Validation;
		}

		/// <summary>
		/// Checks if a command applies in the current state of the token's state machine. A command without a condition,
		/// its note context script, always applies. A condition runs in the background with the current state as
		/// <c>State</c> and the machine's variables, and the command applies only if it returns true. A condition that
		/// isn't allowed, fails, or runs longer than <see cref="ScriptTimeout"/> counts as false.
		/// </summary>
		/// <param name="Command">The note command.</param>
		/// <param name="State">The current state, or null when it isn't known.</param>
		/// <param name="MachineVariables">The state machine's variables. They aren't changed.</param>
		/// <returns>If the command applies.</returns>
		public static async Task<bool> IsAvailableAsync(NoteCommand Command, string? State, IReadOnlyDictionary<string, object> MachineVariables)
		{
			if (string.IsNullOrWhiteSpace(Command.NoteContextScript))
				return true;

			if (string.IsNullOrEmpty(State))
				return false;

			Expression Condition;
			string[] AssignedNames;

			try
			{
				Condition = Command.ParsedNoteContextScript;
				if (Condition is null)
					return false;

				HashSet<string> Names = new(MachineVariables.Keys, StringComparer.Ordinal)
				{
					StateVariable,
					StateTagVariable
				};

				if (!NoteScriptValidator.IsAllowed([Condition], Names, out AssignedNames))
				{
					if (refusedConditions.TryAdd(Command, Command))
					{
						ServiceRef.LogService.LogWarning("Note command " + Command.Id +
							" was not shown, since its condition isn't allowed in the app.");
					}

					return false;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return false;
			}

			Variables Variables = new();

			foreach (string Name in AssignedNames)
				Variables[Name] = null;

			Variables[StateVariable] = State;
			Variables[StateTagVariable] = State;

			foreach (KeyValuePair<string, object> Variable in MachineVariables)
				Variables[Variable.Key] = Variable.Value;

			Task<object> Evaluation = Task.Run(() => Condition.EvaluateAsync(Variables));
			Task Completed = await Task.WhenAny(Evaluation, Task.Delay(ScriptTimeout));

			if (Completed != Evaluation)
			{
				Variables.Abort();
				return false;
			}

			try
			{
				return await Evaluation is bool Result && Result;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return false;
			}
		}

		/// <summary>
		/// Runs a command: asks for confirmation when the command has a confirmation text, generates the note from the
		/// parameter values, checks that the command still applies, and adds the note to the token, as a personal note
		/// when the command says so. Success is shown as a toast, and failure in a dialog, with the command's own texts
		/// when it has them.
		/// </summary>
		/// <param name="Command">The note command. Its parameters hold the values to use.</param>
		/// <param name="TokenId">ID of the token.</param>
		/// <param name="AssignedNames">Names the scripts assign that aren't parameters, from <see cref="CanRun"/>.</param>
		/// <param name="IsAvailable">Checks if the command still applies, just before the note is sent, since the token's
		/// state can change while the user fills in the form. Null to send without checking.</param>
		/// <returns>If the note was added.</returns>
		public static async Task<bool> RunAsync(NoteCommand Command, string TokenId, string[] AssignedNames, Func<Task<bool>>? IsAvailable)
		{
			string Title = SelectText(Command.Title) ?? Command.Id ?? string.Empty;
			string? Confirmation = SelectText(Command.Confirmation);

			if (Confirmation is not null &&
				!await ServiceRef.UiService.DisplayAlert(Title, Confirmation,
					ServiceRef.Localizer[nameof(AppResources.Confirm)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]))
			{
				return false;
			}

			string FailureText = SelectText(Command.Failure) ?? ServiceRef.Localizer[nameof(AppResources.NoteCommandFailed)];
			object? Note;

			try
			{
				Variables Variables = CreateVariables(Command, AssignedNames);
				Note = await GenerateNoteAsync(Command, Variables);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ShowFailureAsync(Title, FailureText);
				return false;
			}

			if (IsAvailable is not null && !await IsAvailable())
			{
				await ShowFailureAsync(Title, ServiceRef.Localizer[nameof(AppResources.NoteCommandNotAvailable)]);
				return false;
			}

			try
			{
				switch (Note)
				{
					case string Text when !string.IsNullOrWhiteSpace(Text):
						await ServiceRef.XmppService.AddNeuroFeatureTextNote(TokenId, Text, Command.Personal);
						break;

					case XmlDocument Doc when Doc.DocumentElement is not null:
						await ServiceRef.XmppService.AddNeuroFeatureXmlNote(TokenId, Doc.DocumentElement.OuterXml, Command.Personal);
						break;

					case XmlElement Element:
						await ServiceRef.XmppService.AddNeuroFeatureXmlNote(TokenId, Element.OuterXml, Command.Personal);
						break;

					default:
						await ShowFailureAsync(Title, FailureText);
						return false;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ShowFailureAsync(Title, TokenErrors.ToMessage(Ex) ?? FailureText);
				return false;
			}

			await ServiceRef.ToastService.ShowTextAsync(SelectText(Command.Success) ?? ServiceRef.Localizer[nameof(AppResources.NoteCommandSent)]);
			return true;
		}

		/// <summary>
		/// Runs the note generation script in the background, and stops it if it runs longer than
		/// <see cref="ScriptTimeout"/>.
		/// </summary>
		/// <param name="Command">The note command.</param>
		/// <param name="Variables">The variables to run the scripts with.</param>
		/// <returns>The generated note.</returns>
		private static async Task<object?> GenerateNoteAsync(NoteCommand Command, Variables Variables)
		{
			Task<object?> Evaluation = Task.Run(async () =>
			{
				object? Note = await Command.GetNote(Variables);
				return Note;
			});

			Task Completed = await Task.WhenAny(Evaluation, Task.Delay(ScriptTimeout));

			if (Completed != Evaluation)
			{
				Variables.Abort();
				throw new TimeoutException("The note command's scripts took too long.");
			}

			return await Evaluation;
		}

		/// <summary>
		/// Tells the user that a command didn't work.
		/// </summary>
		/// <param name="Title">The command's title.</param>
		/// <param name="Message">The message.</param>
		private static Task ShowFailureAsync(string Title, string Message)
		{
			return ServiceRef.UiService.DisplayAlert(Title, Message, ServiceRef.Localizer[nameof(AppResources.Ok)]);
		}

		/// <summary>
		/// Gets the language part of a culture name, such as "sv" in "sv-SE".
		/// </summary>
		/// <param name="CultureName">The culture name.</param>
		/// <returns>The language, or null when the name is empty.</returns>
		private static string? GetLanguage(string? CultureName)
		{
			if (string.IsNullOrEmpty(CultureName))
				return null;

			int i = CultureName.IndexOfAny(['-', '_']);
			return i < 0 ? CultureName : CultureName[..i];
		}
	}
}
