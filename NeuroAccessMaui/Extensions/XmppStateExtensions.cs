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
				XmppState.Offline => Color.Red,

			XmppState.Authenticating or
				XmppState.Connecting or
				XmppState.Registering or
				XmppState.StartingEncryption or
				XmppState.StreamNegotiation or
				XmppState.StreamOpened => Color.Yellow,

			XmppState.Binding or
				XmppState.FetchingRoster or
				XmppState.RequestingSession or
				XmppState.SettingPresence => Blend(Color.Yellow, connectedColor, 0.5),

			XmppState.Connected => connectedColor,

			_ => Color.Gray,
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
		int R = (int)(Color1.R * (1 - p) + Color2.R * p + 0.5);
		int G = (int)(Color1.G * (1 - p) + Color2.G * p + 0.5);
		int B = (int)(Color1.B * (1 - p) + Color2.B * p + 0.5);
		int A = (int)(Color1.A * (1 - p) + Color2.A * p + 0.5);

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
			XmppState.Authenticating => LocalizationResourceManager.Current["XmppState_Authenticating"],
			XmppState.Binding => LocalizationResourceManager.Current["XmppState_Binding"],
			XmppState.Connected => LocalizationResourceManager.Current["XmppState_Connected"],
			XmppState.Connecting => LocalizationResourceManager.Current["XmppState_Connecting"],
			XmppState.Error => LocalizationResourceManager.Current["XmppState_Error"],
			XmppState.FetchingRoster => LocalizationResourceManager.Current["XmppState_FetchingRoster"],
			XmppState.Registering => LocalizationResourceManager.Current["XmppState_Registering"],
			XmppState.RequestingSession => LocalizationResourceManager.Current["XmppState_RequestingSession"],
			XmppState.SettingPresence => LocalizationResourceManager.Current["XmppState_SettingPresence"],
			XmppState.StartingEncryption => LocalizationResourceManager.Current["XmppState_StartingEncryption"],
			XmppState.StreamNegotiation => LocalizationResourceManager.Current["XmppState_StreamNegotiation"],
			XmppState.StreamOpened => LocalizationResourceManager.Current["XmppState_StreamOpened"],
			_ => LocalizationResourceManager.Current["XmppState_Offline"],
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
			IdentityState.Approved => LocalizationResourceManager.Current["IdentityState_Approved"],
			IdentityState.Compromised => LocalizationResourceManager.Current["IdentityState_Compromized"],
			IdentityState.Created => LocalizationResourceManager.Current["IdentityState_Created"],
			IdentityState.Obsoleted => LocalizationResourceManager.Current["IdentityState_Obsoleted"],
			IdentityState.Rejected => LocalizationResourceManager.Current["IdentityState_Rejected"],
			_ => string.Empty,
		};
	}

}
