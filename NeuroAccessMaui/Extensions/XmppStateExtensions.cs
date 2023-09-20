using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Exceptions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Localization;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Extensions;

/// <summary>
/// Extensions for the <see cref="XmppState"/> enum.
/// </summary>
public static class XmppStateExtensions
{
	/// <summary>
	/// Returns a color matching the current connection state.
	/// </summary>
	/// <param name="State">Connection state.</param>
	/// <returns>Color</returns>
	public static Color ToColor(this XmppState State)
	{
		return State switch
		{
			XmppState.Error or
				XmppState.Offline => Colors.Red,

			XmppState.Authenticating or
				XmppState.Connecting or
				XmppState.Registering or
				XmppState.StartingEncryption or
				XmppState.StreamNegotiation or
				XmppState.StreamOpened => Colors.Yellow,

			XmppState.Binding or
				XmppState.FetchingRoster or
				XmppState.RequestingSession or
				XmppState.SettingPresence => Blend(Colors.Yellow, connectedColor, 0.5),

			XmppState.Connected => connectedColor,

			_ => Colors.Gray,
		};
	}

	private static readonly Color connectedColor = Color.FromRgb(146, 208, 80);

	/// <summary>
	/// Blends two colors.
	/// </summary>
	/// <param name="Color1">Color 1</param>
	/// <param name="Color2">Color 2</param>
	/// <param name="p">Blending coefficient (0-1).</param>
	/// <returns>Blended color.</returns>
	public static Color Blend(Color Color1, Color Color2, double p)
	{
		int R = (int)(Color1.Red * (1 - p) + Color2.Red * p + 0.5);
		int G = (int)(Color1.Green * (1 - p) + Color2.Green * p + 0.5);
		int B = (int)(Color1.Blue * (1 - p) + Color2.Blue * p + 0.5);
		int A = (int)(Color1.Alpha * (1 - p) + Color2.Alpha * p + 0.5);

		return new Color(R, G, B, A);
	}

	/// <summary>
	/// Converts the state to a localized string.
	/// </summary>
	/// <param name="State">The state to convert.</param>
	/// <returns>Textual representation of an XMPP connection state.</returns>
	public static string ToDisplayText(this XmppState State)
	{
		IStringLocalizer? Localizer = LocalizationManager.GetStringLocalizer()
			?? throw new LocalizationException("There is no localization service");

		return State switch
		{
			XmppState.Authenticating => Localizer[nameof(AppResources.XmppState_Authenticating)],
			XmppState.Binding => Localizer[nameof(AppResources.XmppState_Binding)],
			XmppState.Connected => Localizer[nameof(AppResources.XmppState_Connected)],
			XmppState.Connecting => Localizer[nameof(AppResources.XmppState_Connecting)],
			XmppState.Error => Localizer[nameof(AppResources.XmppState_Error)],
			XmppState.FetchingRoster => Localizer[nameof(AppResources.XmppState_FetchingRoster)],
			XmppState.Registering => Localizer[nameof(AppResources.XmppState_Registering)],
			XmppState.RequestingSession => Localizer[nameof(AppResources.XmppState_RequestingSession)],
			XmppState.SettingPresence => Localizer[nameof(AppResources.XmppState_SettingPresence)],
			XmppState.StartingEncryption => Localizer[nameof(AppResources.XmppState_StartingEncryption)],
			XmppState.StreamNegotiation => Localizer[nameof(AppResources.XmppState_StreamNegotiation)],
			XmppState.StreamOpened => Localizer[nameof(AppResources.XmppState_StreamOpened)],
			_ => Localizer[nameof(AppResources.XmppState_Offline)],
		};
	}

	/// <summary>
	/// Converts the state to a localized string.
	/// </summary>
	/// <param name="State">The state to convert.</param>
	/// <returns>String representation</returns>
	public static string ToDisplayText(this IdentityState State)
	{
		IStringLocalizer? Localizer = LocalizationManager.GetStringLocalizer()
			?? throw new LocalizationException("There is no localization service");

		return State switch
		{
			IdentityState.Approved => Localizer[nameof(AppResources.IdentityState_Approved)],
			IdentityState.Compromised => Localizer[nameof(AppResources.IdentityState_Compromized)],
			IdentityState.Created => Localizer[nameof(AppResources.IdentityState_Created)],
			IdentityState.Obsoleted => Localizer[nameof(AppResources.IdentityState_Obsoleted)],
			IdentityState.Rejected => Localizer[nameof(AppResources.IdentityState_Rejected)],
			_ => string.Empty,
		};
	}
}
