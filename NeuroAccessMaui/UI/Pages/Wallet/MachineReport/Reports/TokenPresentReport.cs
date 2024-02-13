using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a report of the present state of a token and the underlying state-machine.
	/// </summary>
	/// <param name="TokenId">ID of token being viewed.</param>
	public class TokenPresentReport(string TokenId) : TokenReport(TokenId)
	{
		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(ServiceRef.Localizer[nameof(AppResources.Present)]);

		/// <summary>
		/// Gets the XAML for the report.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public override Task<string> GetReportXaml()
		{
			return ServiceRef.XmppService.GenerateNeuroFeaturePresentReport(this.TokenId);
		}
	}
}
