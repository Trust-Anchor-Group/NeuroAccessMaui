using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Browser link opener.
	/// </summary>
	public class BrowserLink : ILinkOpener
	{
		/// <summary>
		/// Browser link opener.
		/// </summary>
		public BrowserLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			if (Link.Scheme == "http" || Link.Scheme == "https")
				return Grade.Ok;
			else
				return Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link, async () =>
			{
				if (!await Browser.Default.OpenAsync(Link))
					throw new InvalidOperationException("The browser could not open the link.");
			}));
		}
	}
}
