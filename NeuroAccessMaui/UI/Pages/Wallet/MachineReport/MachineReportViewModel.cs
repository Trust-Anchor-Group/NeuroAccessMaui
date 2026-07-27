using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// Displays a live report for one token state-machine.
	/// </summary>
	public partial class MachineReportViewModel : BaseViewModel, IDisposable
	{
		private bool isDisposed;
		private long refreshGeneration;

		/// <summary>
		/// Initializes a state-machine report view.
		/// </summary>
		/// <param name="Args">Navigation arguments containing the report to display.</param>
		public MachineReportViewModel(MachineReportNavigationArgs? Args)
			: base()
		{
			this.TokenReport = Args?.Report;
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			await this.RefreshReportAsync(true);

			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			Interlocked.Increment(ref this.refreshGeneration);
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;
			this.TokenReport?.Dispose();

			return base.OnDisposeAsync();
		}

		private Task Wallet_StateUpdated(
			object? Sender,
			NeuroFeatures.EventArguments.NewStateEventArgs EventArguments)
		{
			if (!this.IsCurrentToken(EventArguments.TokenId))
				return Task.CompletedTask;

			return this.RefreshReportAsync(false);
		}

		private Task Wallet_VariablesUpdated(
			object? Sender,
			NeuroFeatures.EventArguments.VariablesUpdatedEventArgs EventArguments)
		{
			if (!this.IsCurrentToken(EventArguments.TokenId))
				return Task.CompletedTask;

			return this.RefreshReportAsync(false);
		}

		private bool IsCurrentToken(string TokenId)
		{
			return this.TokenReport is not null &&
				!string.IsNullOrWhiteSpace(TokenId) &&
				string.Equals(
					this.TokenReport.TokenId,
					TokenId,
					StringComparison.Ordinal);
		}

		private async Task RefreshReportAsync(bool IsInitialLoad)
		{
			long Generation = Interlocked.Increment(ref this.refreshGeneration);
			TokenReport? TokenReport = this.TokenReport;
			if (TokenReport is null)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					this.ShowUnavailable(
						ServiceRef.Localizer[nameof(AppResources.MachineReportUnavailable)]));
				return;
			}

			if (IsInitialLoad)
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = true);

			try
			{
				string Title = await TokenReport.GetTitle();
				VerticalStackLayout Report = await TokenReport.GetReportMaui();
				if (Generation != Volatile.Read(ref this.refreshGeneration))
					return;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation != Volatile.Read(ref this.refreshGeneration))
						return;

					this.Title = Title;
					this.Report = Report;
					this.Loaded = true;
					this.HasError = false;
					this.ErrorMessage = string.Empty;
				});
			}
			catch (Exception Ex)
			{
				if (Generation != Volatile.Read(ref this.refreshGeneration))
					return;

				// Report failures are logged without remote report content or token identifiers.
				ServiceRef.LogService.LogWarning(
					"Token state-machine report loading failed.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				string Message = this.Loaded
					? ServiceRef.Localizer[nameof(AppResources.MachineReportRefreshFailed)]
					: ServiceRef.Localizer[nameof(AppResources.MachineReportUnavailable)];
				await MainThread.InvokeOnMainThreadAsync(() =>
					this.ShowUnavailable(Message, !this.Loaded));
			}
			finally
			{
				if (Generation == Volatile.Read(ref this.refreshGeneration))
					await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		private void ShowUnavailable(string Message, bool ClearReport = true)
		{
			if (ClearReport)
			{
				this.Loaded = false;
				this.Report = null;
			}

			if (string.IsNullOrWhiteSpace(this.Title))
				this.Title = ServiceRef.Localizer[nameof(AppResources.Reports)];

			this.HasError = true;
			this.ErrorMessage = Message;
		}

		/// <summary>
		/// Retries loading or refreshing the current report.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task RetryReport()
		{
			return this.RefreshReportAsync(!this.Loaded);
		}

		/// <summary>
		/// Gets or sets a value indicating whether a report is available.
		/// </summary>
		[ObservableProperty]
		private bool loaded;

		/// <summary>
		/// Gets or sets a value indicating whether the initial report is loading.
		/// </summary>
		[ObservableProperty]
		private bool isLoading;

		/// <summary>
		/// Gets or sets a value indicating whether report loading or refresh failed.
		/// </summary>
		[ObservableProperty]
		private bool hasError;

		/// <summary>
		/// Gets or sets the localized report failure explanation.
		/// </summary>
		[ObservableProperty]
		private string errorMessage = string.Empty;

		/// <summary>
		/// Gets or sets the localized report title.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Gets or sets the rendered report.
		/// </summary>
		[ObservableProperty]
		private object? report;

		/// <summary>
		/// Gets or sets the report definition.
		/// </summary>
		[ObservableProperty]
		private TokenReport? tokenReport;

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases report resources.
		/// </summary>
		/// <param name="Disposing">Whether managed resources should be released.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
				this.TokenReport?.Dispose();

			this.isDisposed = true;
		}
	}
}
