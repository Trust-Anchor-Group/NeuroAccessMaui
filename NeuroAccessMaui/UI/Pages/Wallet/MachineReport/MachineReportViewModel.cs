using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public partial class MachineReportViewModel : BaseViewModel, IDisposable
	{
		private bool isDisposed;

		[ObservableProperty]
		private bool loaded = false;

		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public MachineReportViewModel(MachineReportNavigationArgs? Args)
			: base()
		{
			this.TokenReport = Args?.Report;
			this.Loaded = false;
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.TokenReport is null)
				this.Title = string.Empty;
			else
			{
				this.Title = await this.TokenReport.GetTitle();

				await this.TokenReport.GenerateReportMaui(this);
				//await this.TokenReport.GenerateReport(this);

				MainThread.BeginInvokeOnMainThread(() =>
					this.Loaded = true);
			}

			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;

			this.DeleteTemporaryFiles();

			return base.OnDisposeAsync();
		}

		private Task Wallet_StateUpdated(object? Sender, NeuroFeatures.EventArguments.NewStateEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task Wallet_VariablesUpdated(object? Sender, NeuroFeatures.EventArguments.VariablesUpdatedEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task UpdateReport()
		{
			return this.TokenReport?.GenerateReport(this) ?? Task.CompletedTask;
		}

		#region Properties

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private object? report;

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private TokenReport? tokenReport;

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
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
				this.DeleteTemporaryFiles();

			this.isDisposed = true;
		}

		private void DeleteTemporaryFiles()
		{
			this.TokenReport?.DeleteTemporaryFiles();
		}

		#endregion

	}
}
