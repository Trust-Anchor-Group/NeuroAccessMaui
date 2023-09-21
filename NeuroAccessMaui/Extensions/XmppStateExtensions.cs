using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
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
		return State switch
		{
			XmppState.Authenticating => ServiceRef.Localizer[nameof(AppResources.XmppState_Authenticating)],
			XmppState.Binding => ServiceRef.Localizer[nameof(AppResources.XmppState_Binding)],
			XmppState.Connected => ServiceRef.Localizer[nameof(AppResources.XmppState_Connected)],
			XmppState.Connecting => ServiceRef.Localizer[nameof(AppResources.XmppState_Connecting)],
			XmppState.Error => ServiceRef.Localizer[nameof(AppResources.XmppState_Error)],
			XmppState.FetchingRoster => ServiceRef.Localizer[nameof(AppResources.XmppState_FetchingRoster)],
			XmppState.Registering => ServiceRef.Localizer[nameof(AppResources.XmppState_Registering)],
			XmppState.RequestingSession => ServiceRef.Localizer[nameof(AppResources.XmppState_RequestingSession)],
			XmppState.SettingPresence => ServiceRef.Localizer[nameof(AppResources.XmppState_SettingPresence)],
			XmppState.StartingEncryption => ServiceRef.Localizer[nameof(AppResources.XmppState_StartingEncryption)],
			XmppState.StreamNegotiation => ServiceRef.Localizer[nameof(AppResources.XmppState_StreamNegotiation)],
			XmppState.StreamOpened => ServiceRef.Localizer[nameof(AppResources.XmppState_StreamOpened)],
			_ => ServiceRef.Localizer[nameof(AppResources.XmppState_Offline)],
		};
	}

	/// <summary>
	/// Converts the state to a localized string.
	/// </summary>
	/// <param name="State">The state to convert.</param>
	/// <returns>String representation</returns>
	public static string ToDisplayText(this IdentityState State)
	{
		return State switch
		{
			IdentityState.Approved => ServiceRef.Localizer[nameof(AppResources.IdentityState_Approved)],
			IdentityState.Compromised => ServiceRef.Localizer[nameof(AppResources.IdentityState_Compromised)],
			IdentityState.Created => ServiceRef.Localizer[nameof(AppResources.IdentityState_Created)],
			IdentityState.Obsoleted => ServiceRef.Localizer[nameof(AppResources.IdentityState_Obsoleted)],
			IdentityState.Rejected => ServiceRef.Localizer[nameof(AppResources.IdentityState_Rejected)],
			_ => string.Empty,
		};
	}
}
