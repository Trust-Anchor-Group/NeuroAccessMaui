using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens e-mail links with the operating system.
	/// </summary>
	/// <remarks>
	/// Links can come from untrusted sources such as chat messages and NFC tags, so only schemes known to be harmless are passed
	/// to the operating system. Schemes such as <c>file</c>, <c>content</c>, or other applications' schemes are not opened.
	/// </remarks>
	public class DefaultLink : ILinkOpener
	{
		/// <summary>
		/// Default link opener.
		/// </summary>
		public DefaultLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Equals(Link.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase) ? Grade.Barely : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link, async () =>
			{
				if (!await Launcher.TryOpenAsync(Link))
					throw new InvalidOperationException("The operating system could not open the link.");
			}));
		}
	}
}
