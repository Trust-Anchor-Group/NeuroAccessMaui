using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public partial class MachineReportViewModel : BaseViewModel, IDisposable
	{
		private bool isDisposed;

		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public MachineReportViewModel(MachineReportNavigationArgs? Args)
			: base()
		{
			this.TokenReport = Args?.Report;
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
				this.ReportTask.Load(this.LoadReportAsync);
			}

			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;

			this.TokenReport?.StopTimer();

			return base.OnDisposeAsync();
		}

		private Task LoadReportAsync(TaskContext<int> Context)
		{
			if (this.TokenReport is null)
				return Task.CompletedTask;

			return this.TokenReport.GenerateReportMaui(this);
		}

		private bool IsForThisToken(StateMachineEventArgs e)
		{
			return this.TokenReport is not null && string.Equals(e.TokenId, this.TokenReport.TokenId, StringComparison.OrdinalIgnoreCase);
		}

		private Task Wallet_StateUpdated(object? Sender, NewStateEventArgs e)
		{
			if (!this.IsForThisToken(e))
				return Task.CompletedTask;

			return this.TokenReport!.OnNewState(Sender, e);
		}

		private Task Wallet_VariablesUpdated(object? Sender, VariablesUpdatedEventArgs e)
		{
			if (!this.IsForThisToken(e))
				return Task.CompletedTask;

			return this.TokenReport!.OnVariablesUpdated(Sender, e);
		}

		#region Properties

		/// <summary>
		/// Loads the report, and exposes its loading, error and loaded states.
		/// </summary>
		public ObservableTask ReportTask { get; } = new();

		/// <summary>
		/// Title of the report.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Rendered report from state-machine.
		/// </summary>
		[ObservableProperty]
		private object? report;

		/// <summary>
		/// Report being displayed.
		/// </summary>
		[ObservableProperty]
		private TokenReport? tokenReport;

		#endregion

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
			{
				this.TokenReport?.Dispose();
				this.ReportTask.Dispose();
			}

			this.isDisposed = true;
		}
	}
}
