using EDaler.Uris;
using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Wallet
{
	/// <summary>
	/// Holds navigation parameters specific to eDaler URIs.
	/// </summary>
	public class EDalerUriNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		public EDalerUriNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		/// <param name="Uri">Parsed edaler URI.</param>
		public EDalerUriNavigationArgs(EDalerUri Uri)
			: this(Uri, string.Empty)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		/// <param name="Uri">Parsed edaler URI.</param>
		/// <param name="FriendlyName">Optional Friendly Name associated with URI</param>
		public EDalerUriNavigationArgs(EDalerUri Uri, string FriendlyName)
			: this(Uri, FriendlyName, null)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		/// <param name="Uri">Parsed edaler URI.</param>
		/// <param name="FriendlyName">Optional Friendly Name associated with URI</param>
		/// <param name="UriToSend">Task Completion Source in case the URI being built is to be returned to the parent page.</param>
		public EDalerUriNavigationArgs(EDalerUri Uri, string FriendlyName, TaskCompletionSource<string?>? UriToSend)
		{
			this.Uri = Uri;
			this.FriendlyName = FriendlyName;
			this.UriToSend = UriToSend;
		}

		/// <summary>
		/// The edaler URI
		/// </summary>
		public EDalerUri? Uri { get; }

		/// <summary>
		/// Optional Friendly Name associated with URI
		/// </summary>
		public string? FriendlyName { get; }

		/// <summary>
		/// Task Completion Source in case the URI being built is to be returned to the parent page.
		/// </summary>
		public TaskCompletionSource<string?>? UriToSend { get; }
	}
}
