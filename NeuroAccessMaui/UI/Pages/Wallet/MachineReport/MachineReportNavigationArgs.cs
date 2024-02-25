using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// Holds navigation parameters specific to a report from a state-machine.
	/// </summary>
	public class MachineReportNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MachineReportNavigationArgs"/> class.
		/// </summary>
		public MachineReportNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MachineReportNavigationArgs"/> class.
		/// </summary>
		public MachineReportNavigationArgs(TokenReport Report)
		{
			this.Report = Report;
		}

		/// <summary>
		/// Report to display.
		/// </summary>
		public TokenReport? Report { get; }
	}
}
