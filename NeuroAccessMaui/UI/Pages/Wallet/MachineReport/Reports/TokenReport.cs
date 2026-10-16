using NeuroAccessMaui.Services;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Abstract base class for token reports.
	/// </summary>
	/// <param name="TokenId">ID of token being viewed.</param>
	public abstract class TokenReport(string TokenId)
		: IDisposable
	{
		private readonly string tokenId = TokenId;
		private MachineReportViewModel? view;
		private Timer? timer;
		private bool isDisposed;

		/// <summary>
		/// Token ID associated with the state-machine.
		/// </summary>
		public string TokenId => this.tokenId;

		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public abstract Task<string> GetTitle();

		/// <summary>
		/// Gets the MAUI representation of the report.
		/// </summary>
		/// <returns>MAUI representation of report.</returns>
		public abstract Task<VerticalStackLayout> GetReportMaui();

		/// <summary>
		/// Method called when the underlying state-machine has changed state.
		/// </summary>
		/// <param name="Sender">Sender of event.</param>
		/// <param name="e">Event arguments.</param>
		public virtual Task OnNewState(object? Sender, NewStateEventArgs e)
		{
			this.DelayedRefresh();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Method called when variables in the underlying state-machine have been updated.
		/// </summary>
		/// <param name="Sender">Sender of event.</param>
		/// <param name="e">Event arguments.</param>
		public virtual Task OnVariablesUpdated(object? Sender, VariablesUpdatedEventArgs e)
		{
			this.DelayedRefresh();
			return Task.CompletedTask;
		}

		private void DelayedRefresh()
		{
			this.StopTimer();
			this.timer = new Timer(this.Action, null, 500, Timeout.Infinite);
		}

		/// <summary>
		/// Method called when it is time to execute action.
		/// </summary>
		public async void Action(object? _)
		{
			if (this.view is null || this.isDisposed)
				return;

			try
			{
				await this.GenerateReportMaui(this.view);
			}
			catch (Exception ex)
			{
				// A refresh failure keeps the report that is already shown.
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Generates the report using the MAUI renderer. The report is fetched and rendered off the UI thread,
		/// and then shown on the main thread.
		/// </summary>
		/// <param name="ReportView">View model showing the report.</param>
		public async Task GenerateReportMaui(MachineReportViewModel ReportView)
		{
			this.view = ReportView;

			VerticalStackLayout MauiContent = await Task.Run(() => this.GetReportMaui());

			await MainThread.InvokeOnMainThreadAsync(() => ReportView.Report = MauiContent);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
				this.StopTimer();

			this.isDisposed = true;
		}

		/// <summary>
		/// Stops the timer for any queued delayed refresh action.
		/// </summary>
		public void StopTimer()
		{
			this.timer?.Dispose();
			this.timer = null;
		}
	}
}
