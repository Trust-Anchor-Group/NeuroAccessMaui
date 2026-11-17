using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Converters;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A step of a token's flow: its name, if the token is in it now, what moves it on from there, and how it gets there.
	/// </summary>
	/// <param name="State">The step.</param>
	/// <param name="Names">The names in words of all steps, by code name.</param>
	public partial class TokenFlowStepItem(TokenFlowState State, IReadOnlyDictionary<string, string> Names)
		: ObservableObject
	{
		private readonly TokenFlowState state = State;
		private readonly IReadOnlyDictionary<string, string> names = Names;

		/// <summary>
		/// The step's code name.
		/// </summary>
		public string Id => this.state.Id;

		/// <summary>
		/// The step's name in words.
		/// </summary>
		public string Name => this.names.TryGetValue(this.state.Id, out string? Words) ? Words : TokenText.ToWords(this.state.Id);

		/// <summary>
		/// If the flow ends in this step.
		/// </summary>
		public bool IsEnd => this.state.IsEnd;

		/// <summary>
		/// The ways out of the step.
		/// </summary>
		public IReadOnlyList<TokenFlowExit> Exits => this.state.Exits;

		/// <summary>
		/// If the token is in this step now, or ended its flow in it.
		/// </summary>
		[ObservableProperty]
		private bool isCurrent;

		/// <summary>
		/// If the token's flow has ended in this step.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BadgeText))]
		private bool isFinished;

		/// <summary>
		/// The badge on the step the token is in: "Now", or "Ended" when the flow ended there.
		/// </summary>
		public string BadgeText => this.IsFinished
			? ServiceRef.Localizer[nameof(AppResources.StateEnded)]
			: ServiceRef.Localizer[nameof(AppResources.FlowNow)];

		/// <summary>
		/// If the token only comes to this step when something fails. Its dot is dashed and its name dimmed.
		/// </summary>
		public bool IsFailureStep { get; init; }

		/// <summary>
		/// If the line of the flow runs on below this step, to the next step shown.
		/// </summary>
		[ObservableProperty]
		private bool hasRailBelow;

		/// <summary>
		/// How the token gets to this step from the one it is in now, such as "When a condition is met".
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasIncomingText))]
		private string incomingText = string.Empty;

		/// <summary>
		/// If there is a text about how the token gets to this step.
		/// </summary>
		public bool HasIncomingText => !string.IsNullOrEmpty(this.IncomingText);

		/// <summary>
		/// What moves the token on from this step, one line per way out with an icon. Filled for the step it is in now.
		/// </summary>
		public ObservableCollection<TokenFlowWhenLine> WhenLines { get; } = [];

		/// <summary>
		/// If the ways out are listed as lines.
		/// </summary>
		public bool HasWhenLines => this.WhenLines.Count > 0;

		/// <summary>
		/// Lists what moves the token on from this step, or clears the list.
		/// </summary>
		/// <param name="Variables">The token's values now, by name, or null to clear the list.</param>
		public void ShowWhenLines(IReadOnlyDictionary<string, object>? Variables)
		{
			this.WhenLines.Clear();

			if (Variables is not null)
			{
				TokenFlowExit[] Exits = [.. this.state.Exits.Distinct()];
				bool ManyTargets = Exits.Select(Exit => Exit.Target).Distinct().Count() > 1;

				foreach (TokenFlowExit Exit in Exits)
				{
					string Target = this.NameOf(Exit.Target);
					string Text;

					if (Exit.IsFailure)
						Text = ServiceRef.Localizer[nameof(AppResources.FlowIfFails), Target];
					else
					{
						Text = DescribeTrigger(Exit, Variables);
						if (ManyTargets)
							Text += " → " + Target;

						// The second way out and those after it read "Or when ...".
						if (this.WhenLines.Count > 0)
							Text = ServiceRef.Localizer[nameof(AppResources.FlowOr), LowerFirst(Text)];
					}

					this.WhenLines.Add(new TokenFlowWhenLine(IconOf(Exit), Text));
				}
			}

			this.OnPropertyChanged(nameof(this.HasWhenLines));
		}

		/// <summary>
		/// Describes how the token gets into a step through a way out of another step.
		/// </summary>
		/// <param name="Exit">The way out that leads to the step.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>The description, such as "When a condition is met" or "If something goes wrong".</returns>
		public static string DescribeWayIn(TokenFlowExit Exit, IReadOnlyDictionary<string, object> Variables)
		{
			return Exit.IsFailure
				? ServiceRef.Localizer[nameof(AppResources.FlowIncomingFailure)]
				: DescribeTrigger(Exit, Variables);
		}

		/// <summary>
		/// Gets the step name in words for a code name.
		/// </summary>
		/// <param name="Id">The step's code name.</param>
		/// <returns>The name in words.</returns>
		private string NameOf(string Id)
		{
			return this.names.TryGetValue(Id, out string? Words) ? Words : TokenText.ToWords(Id);
		}

		/// <summary>
		/// Gets the icon for the kind of trigger of a way out.
		/// </summary>
		/// <param name="Exit">The way out.</param>
		/// <returns>The icon.</returns>
		public static Geometry IconOf(TokenFlowExit Exit)
		{
			if (Exit.IsFailure)
				return Geometries.ErrorPath;

			return Exit.Trigger switch
			{
				TokenFlowTrigger.DateTime => Geometries.CalendarPath,
				TokenFlowTrigger.Duration => Geometries.DurationPath,
				TokenFlowTrigger.Condition => Geometries.CalculatorPath,
				TokenFlowTrigger.Transferred => Geometries.SendPath,
				TokenFlowTrigger.PaymentReceived or TokenFlowTrigger.PaymentSent => Geometries.MoneyPath,
				TokenFlowTrigger.OwnerNote => Geometries.TapPath,
				TokenFlowTrigger.ExternalNote => Geometries.PersonPath,
				TokenFlowTrigger.ContractSigned => Geometries.ContractPath,
				TokenFlowTrigger.Destroyed => Geometries.TrashCanButtonPath,
				TokenFlowTrigger.TemplateApproved => Geometries.SuccessCheckmarkPath,
				TokenFlowTrigger.EntryAdded or TokenFlowTrigger.EntryUpdated or TokenFlowTrigger.EntryDeleted => Geometries.ContractEditPath,
				_ => Geometries.InfoCirclePath
			};
		}

		/// <summary>
		/// Makes the first letter of a text lower case, so it can follow a word such as "Or".
		/// </summary>
		/// <param name="Text">The text.</param>
		/// <returns>The text with a lower-case first letter.</returns>
		private static string LowerFirst(string Text)
		{
			return string.IsNullOrEmpty(Text) ? Text : char.ToLower(Text[0], CultureInfo.CurrentCulture) + Text[1..];
		}

		/// <summary>
		/// Describes what triggers a way out, in plain words.
		/// </summary>
		/// <param name="Exit">The way out.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>The description.</returns>
		public static string DescribeTrigger(TokenFlowExit Exit, IReadOnlyDictionary<string, object> Variables)
		{
			switch (Exit.Trigger)
			{
				case TokenFlowTrigger.DateTime:
					DateTime? When = ToDateTime(Resolve(Exit.Argument, Variables));
					if (When is DateTime Time)
					{
						DateTime Local = Time.Kind == DateTimeKind.Utc ? Time.ToLocalTime() : Time;
						return ServiceRef.Localizer[nameof(AppResources.FlowOnDate), Local.ToString("g", CultureInfo.CurrentCulture)];
					}
					return ServiceRef.Localizer[nameof(AppResources.FlowOnSetDate)];

				case TokenFlowTrigger.Duration:
					Duration? Length = ToDuration(Resolve(Exit.Argument, Variables));
					if (Length is Duration Span)
						return ServiceRef.Localizer[nameof(AppResources.FlowAfter), DurationToString.ToString(Span)];
					return ServiceRef.Localizer[nameof(AppResources.FlowAfterSetTime)];

				case TokenFlowTrigger.Condition:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenCondition)];

				case TokenFlowTrigger.Transferred:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenSold)];

				case TokenFlowTrigger.PaymentReceived:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenPaid)];

				case TokenFlowTrigger.PaymentSent:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenPaymentSent)];

				case TokenFlowTrigger.OwnerNote:
					return string.IsNullOrEmpty(Exit.Argument)
						? ServiceRef.Localizer[nameof(AppResources.FlowWhenOwnerNotes)]
						: ServiceRef.Localizer[nameof(AppResources.FlowWhenOwnerTaps), TokenText.ToWords(Exit.Argument)];

				case TokenFlowTrigger.ExternalNote:
					return string.IsNullOrEmpty(Exit.Argument)
						? ServiceRef.Localizer[nameof(AppResources.FlowWhenOthersNote)]
						: ServiceRef.Localizer[nameof(AppResources.FlowWhenOthersTap), TokenText.ToWords(Exit.Argument)];

				case TokenFlowTrigger.ContractSigned:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenContractSigned)];

				case TokenFlowTrigger.Destroyed:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenDestroyed)];

				case TokenFlowTrigger.TemplateApproved:
					return string.IsNullOrEmpty(Exit.Argument)
						? ServiceRef.Localizer[nameof(AppResources.FlowWhenAnyTemplateApproved)]
						: ServiceRef.Localizer[nameof(AppResources.FlowWhenTemplateApproved), TokenText.ToWords(Exit.Argument)];

				case TokenFlowTrigger.EntryAdded:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenEntryAdded)];

				case TokenFlowTrigger.EntryUpdated:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenEntryUpdated)];

				case TokenFlowTrigger.EntryDeleted:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenEntryDeleted)];

				default:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenEvent)];
			}
		}

		/// <summary>
		/// Says what triggers a way out in a few words, for a label on the flow map next to the trigger's icon, such as
		/// "Ship order" or "14 days". <see cref="DescribeTrigger"/> gives the whole sentence.
		/// </summary>
		/// <param name="Exit">The way out.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>The short label.</returns>
		public static string ShortTrigger(TokenFlowExit Exit, IReadOnlyDictionary<string, object> Variables)
		{
			if (Exit.IsFailure)
				return ServiceRef.Localizer[nameof(AppResources.FlowShortFails)];

			switch (Exit.Trigger)
			{
				case TokenFlowTrigger.DateTime:
					DateTime? When = ToDateTime(Resolve(Exit.Argument, Variables));
					if (When is DateTime Time)
					{
						DateTime Local = Time.Kind == DateTimeKind.Utc ? Time.ToLocalTime() : Time;
						return Local.ToString("M", CultureInfo.CurrentCulture);
					}
					return ServiceRef.Localizer[nameof(AppResources.FlowShortSetDate)];

				case TokenFlowTrigger.Duration:
					Duration? Length = ToDuration(Resolve(Exit.Argument, Variables));
					return Length is Duration Span
						? DurationToString.ToString(Span)
						: ServiceRef.Localizer[nameof(AppResources.FlowShortSetTime)];

				case TokenFlowTrigger.Condition:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortCondition)];

				case TokenFlowTrigger.Transferred:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortSold)];

				case TokenFlowTrigger.PaymentReceived:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortPaid)];

				case TokenFlowTrigger.PaymentSent:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortPaymentSent)];

				case TokenFlowTrigger.OwnerNote:
					return string.IsNullOrEmpty(Exit.Argument)
						? ServiceRef.Localizer[nameof(AppResources.FlowShortOwnerNote)]
						: TokenText.ToWords(Exit.Argument);

				case TokenFlowTrigger.ExternalNote:
					return string.IsNullOrEmpty(Exit.Argument)
						? ServiceRef.Localizer[nameof(AppResources.FlowShortOthersNote)]
						: TokenText.ToWords(Exit.Argument);

				case TokenFlowTrigger.ContractSigned:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortSigned)];

				case TokenFlowTrigger.Destroyed:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortDestroyed)];

				case TokenFlowTrigger.TemplateApproved:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortTemplateApproved)];

				case TokenFlowTrigger.EntryAdded:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortEntryAdded)];

				case TokenFlowTrigger.EntryUpdated:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortEntryUpdated)];

				case TokenFlowTrigger.EntryDeleted:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortEntryDeleted)];

				default:
					return ServiceRef.Localizer[nameof(AppResources.FlowShortEvent)];
			}
		}

		/// <summary>
		/// Reads a date and time from a value or a text.
		/// </summary>
		private static DateTime? ToDateTime(object? Value)
		{
			if (Value is DateTime Time)
				return Time;

			if (Value is string Text && DateTime.TryParse(Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Parsed))
				return Parsed;

			return null;
		}

		/// <summary>
		/// Reads a duration from a value or a text such as "PT10S".
		/// </summary>
		private static Duration? ToDuration(object? Value)
		{
			if (Value is Duration Length)
				return Length;

			if (Value is string Text && Duration.TryParse(Text, out Duration Parsed))
				return Parsed;

			return null;
		}

		/// <summary>
		/// Gets the value a trigger's argument stands for. An argument such as "{Expires}" names one of the token's values.
		/// </summary>
		/// <param name="Argument">The argument.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>The value, the argument itself, or null.</returns>
		private static object? Resolve(string? Argument, IReadOnlyDictionary<string, object> Variables)
		{
			if (string.IsNullOrEmpty(Argument))
				return null;

			if (Argument.StartsWith('{') && Argument.EndsWith('}'))
			{
				string Name = Argument[1..^1].Trim();
				return Variables.TryGetValue(Name, out object? Value) ? Value : null;
			}

			return Argument;
		}
	}
}
