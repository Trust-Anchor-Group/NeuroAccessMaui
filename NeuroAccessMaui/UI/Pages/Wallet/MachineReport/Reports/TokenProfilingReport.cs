using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a profiling report of a token and the underlying state-machine.
	/// </summary>
	/// <param name="TokenId">ID of token being viewed.</param>
	public class TokenProfilingReport(string TokenId) : TokenReport(TokenId)
	{
		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(ServiceRef.Localizer[nameof(AppResources.Profiling)]);


		/// <summary>
		/// Gets the MAUI representation of the report.
		/// </summary>
		/// <returns>MAUI representation of the report.</returns>
		public override Task<VerticalStackLayout> GetReportMaui()
		{
			return ServiceRef.XmppService.GenerateNeuroFeatureProfilingReportMaui(this.TokenId);
		}
	}
}
