using System.Text;
using Waher.Networking.Sniffers;

namespace NeuroAccessMaui.Extensions;

/// <summary>
/// Extensions for the <see cref="ISniffer"/> implementation.
/// </summary>
public static class SnifferExtensions
{
	/// <summary>
	/// Converts the latest Xmpp communication that the sniffer holds to plain text.
	/// </summary>
	/// <param name="sniffer">The sniffer whose contents to get.</param>
	/// <returns>The xmpp communication in plain text.</returns>
	public static async Task<string> SnifferToText(this InMemorySniffer sniffer)
	{
		if (sniffer is null)
			return string.Empty;

		StringBuilder sb = new();

		using (StringWriter writer = new(sb))
		using (TextWriterSniffer output = new(writer, BinaryPresentationMethod.ByteCount))
		{
			await sniffer.ReplayAsync(output);
		}

		return sb.ToString();
	}
}
