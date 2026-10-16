using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a contract's state to localized text. Bind to an <see cref="ObservableContract"/> or a <see cref="Contract"/>
	/// to tell approved templates ("Approved") from approved contracts ("Ready to sign"). A bare <see cref="ContractState"/>
	/// is treated as a contract.
	/// </summary>
	internal class ContractStateToString : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value switch
			{
				ObservableContract Observable => GetText(Observable.ContractState, Observable.IsTemplate),
				Contract Contract => GetText(Contract.State, Contract.PartsMode == ContractParts.TemplateOnly),
				ContractState State => GetText(State, false),
				_ => string.Empty
			};
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// Converting back is not typically necessary in this case
			return Binding.DoNothing;
		}

		/// <summary>
		/// Gets the localized text for a contract state.
		/// </summary>
		/// <param name="State">The contract state.</param>
		/// <param name="IsTemplate">If the contract is a template, which can't be signed.</param>
		/// <returns>The localized text.</returns>
		internal static string GetText(ContractState State, bool IsTemplate)
		{
			string Key = State switch
			{
				ContractState.Proposed => nameof(AppResources.Proposed),
				ContractState.Rejected => nameof(AppResources.Rejected),
				ContractState.Approved => IsTemplate ? nameof(AppResources.Approved) : nameof(AppResources.ContractStateReadyToSign),
				ContractState.BeingSigned => nameof(AppResources.BeingSigned),
				ContractState.Signed => nameof(AppResources.Signed),
				ContractState.Failed => nameof(AppResources.Failed),
				ContractState.Obsoleted => nameof(AppResources.Obsoleted),
				ContractState.Deleted => nameof(AppResources.Deleted),
				_ => string.Empty
			};

			return string.IsNullOrEmpty(Key) ? string.Empty : ServiceRef.Localizer[Key];
		}
	}
}
