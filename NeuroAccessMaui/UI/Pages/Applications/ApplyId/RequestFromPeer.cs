using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{
	/// <summary>
	/// Represents representing a peer review from a peer, by scanning their QR-code.
	/// </summary>
	public class RequestFromPeer : ServiceProviderWithLegalId
	{
		/// <summary>
		/// Represents representing a peer review from a peer, by scanning their QR-code.
		/// </summary>
		public RequestFromPeer()
			: base(string.Empty,string.Empty, ServiceRef.Localizer[nameof(AppResources.RequestReviewFromAPeer)],
				  string.Empty, true, Constants.Images.Qr_Person, 230, 230)
		{
		}
	}
}
