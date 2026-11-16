using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Converters;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A step of a token's flow, shown as a row: its name, if the token is in it now, and what moves it on from there.
	/// </summary>
	/// <param name="State">The step.</param>
	/// <param name="Names">The names in words of all steps, by code name.</param>
	public partial class TokenFlowStepItem(TokenFlowState State, IReadOnlyDictionary<string, string> Names)
		: ObservableObject, IListCardItem
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
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// If the token is in this step now.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasExitsText))]
		private bool isCurrent;

		/// <summary>
		/// What moves the token on from this step, one way per line, such as "On 1 Jun 2026 → Done".
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasExitsText))]
		private string exitsText = string.Empty;

		/// <summary>
		/// If the ways out are shown: for the step the token is in, and in the full flow.
		/// </summary>
		public bool HasExitsText => !string.IsNullOrEmpty(this.ExitsText);

		/// <summary>
		/// Works out the text about the ways out of the step, with dates and times taken from the token's values.
		/// </summary>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>One line per way out, or the end of the flow.</returns>
		public string DescribeExits(IReadOnlyDictionary<string, object> Variables)
		{
			if (this.state.IsEnd)
				return ServiceRef.Localizer[nameof(AppResources.FlowStaysHere)];

			List<string> Lines = [];

			foreach (TokenFlowExit Exit in this.state.Exits)
			{
				string Target = this.names.TryGetValue(Exit.Target, out string? Words) ? Words : TokenText.ToWords(Exit.Target);
				string Trigger = DescribeTrigger(Exit, Variables);

				Lines.Add(Exit.IsFailure
					? ServiceRef.Localizer[nameof(AppResources.FlowIfFails), Target]
					: Trigger + " → " + Target);
			}

			return string.Join(Environment.NewLine, Lines.Distinct());
		}

		/// <summary>
		/// Describes what triggers a way out, in plain words.
		/// </summary>
		/// <param name="Exit">The way out.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		/// <returns>The description.</returns>
		private static string DescribeTrigger(TokenFlowExit Exit, IReadOnlyDictionary<string, object> Variables)
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

				default:
					return ServiceRef.Localizer[nameof(AppResources.FlowWhenEvent)];
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
