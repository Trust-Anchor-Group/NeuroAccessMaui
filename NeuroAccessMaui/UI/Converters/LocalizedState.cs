using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts an enumerated state into a localized string.
	/// </summary>
	[AcceptEmptyServiceProvider]
	public class LocalizedState : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
		{
			if (Value is IdentityState IdentityState)
			{
				switch (IdentityState)
				{
					case IdentityState.Created: return ServiceRef.Localizer[nameof(AppResources.Created)];
					case IdentityState.Rejected: return ServiceRef.Localizer[nameof(AppResources.Rejected)];
					case IdentityState.Approved: return ServiceRef.Localizer[nameof(AppResources.Approved)];
					case IdentityState.Obsoleted: return ServiceRef.Localizer[nameof(AppResources.Obsoleted)];
					case IdentityState.Compromised: return ServiceRef.Localizer[nameof(AppResources.Compromised)];
				}
			}
			else if (Value is ContractState ContractState)
			{
				switch (ContractState)
				{
					case ContractState.Proposed: return ServiceRef.Localizer[nameof(AppResources.Proposed)];
					case ContractState.Rejected: return ServiceRef.Localizer[nameof(AppResources.Rejected)];
					case ContractState.Approved: return ServiceRef.Localizer[nameof(AppResources.Approved)];
					case ContractState.BeingSigned: return ServiceRef.Localizer[nameof(AppResources.BeingSigned)];
					case ContractState.Signed: return ServiceRef.Localizer[nameof(AppResources.Signed)];
					case ContractState.Failed: return ServiceRef.Localizer[nameof(AppResources.Failed)];
					case ContractState.Obsoleted: return ServiceRef.Localizer[nameof(AppResources.Obsoleted)];
					case ContractState.Deleted: return ServiceRef.Localizer[nameof(AppResources.Deleted)];
				}
			}
			else if (Value is ContractVisibility ContractVisibility)
			{
				switch (ContractVisibility)
				{
					case ContractVisibility.CreatorAndParts: return ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)];
					case ContractVisibility.DomainAndParts: return ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)];
					case ContractVisibility.Public: return ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)];
					case ContractVisibility.PublicSearchable: return ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)];
				}
			}
			else if (Value is XmppState XmppState)
			{
				switch (XmppState)
				{
					case XmppState.Offline: return ServiceRef.Localizer[nameof(AppResources.Offline)];
					case XmppState.Connecting: return ServiceRef.Localizer[nameof(AppResources.Connecting)];
					case XmppState.StreamNegotiation: return ServiceRef.Localizer[nameof(AppResources.StreamNegotiation)];
					case XmppState.StreamOpened: return ServiceRef.Localizer[nameof(AppResources.StreamOpened)];
					case XmppState.StartingEncryption: return ServiceRef.Localizer[nameof(AppResources.StartingEncryption)];
					case XmppState.Authenticating: return ServiceRef.Localizer[nameof(AppResources.Authenticating)];
					case XmppState.Registering: return ServiceRef.Localizer[nameof(AppResources.Registering)];
					case XmppState.Binding: return ServiceRef.Localizer[nameof(AppResources.Binding)];
					case XmppState.RequestingSession: return ServiceRef.Localizer[nameof(AppResources.RequestingSession)];
					case XmppState.FetchingRoster: return ServiceRef.Localizer[nameof(AppResources.FetchingRoster)];
					case XmppState.SettingPresence: return ServiceRef.Localizer[nameof(AppResources.SettingPresence)];
					case XmppState.Connected: return ServiceRef.Localizer[nameof(AppResources.Connected)];
					case XmppState.Error: return ServiceRef.Localizer[nameof(AppResources.Error)];
				}
			}

			return Value?.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
		{
			return Value?.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ProvideValue(System.IServiceProvider ServiceProvider)
		{
			return this;
		}
	}
}
