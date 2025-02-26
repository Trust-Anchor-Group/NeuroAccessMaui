﻿using System.Text;
using Waher.Networking.Sniffers;

namespace NeuroAccessMaui.Extensions
{
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
		public static async Task<string> SnifferToTextAsync(this InMemorySniffer sniffer)
		{
			if (sniffer is null)
				return string.Empty;

			StringBuilder sb = new();

			await using StringWriter writer = new(sb);
			await using TextWriterSniffer output = new(writer, BinaryPresentationMethod.ByteCount);

			sniffer.Replay(output);

			await sniffer.FlushAsync();

			return sb.ToString();
		}
	}
}
