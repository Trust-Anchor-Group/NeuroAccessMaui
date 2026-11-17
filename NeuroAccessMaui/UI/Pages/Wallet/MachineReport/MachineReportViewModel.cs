using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
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
		/// The report's image, when the report is one image, such as a diagram or a chart. It is then shown where it can be
		/// zoomed and shared straight away.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsImageReport))]
		[NotifyPropertyChangedFor(nameof(IsTextReport))]
		[NotifyPropertyChangedFor(nameof(CanShare))]
		private ImageSource? reportImage;

		/// <summary>
		/// If the report is one image.
		/// </summary>
		public bool IsImageReport => this.ReportImage is not null;

		/// <summary>
		/// If the report is shown as text, with any images in it.
		/// </summary>
		public bool IsTextReport => !this.IsImageReport;

		/// <summary>
		/// If the report's image can be shared.
		/// </summary>
		public bool CanShare => ImageShare.GetFile(this.ReportImage) is not null;

		/// <summary>
		/// Where the report comes from and when, such as "From the server at 10:42".
		/// </summary>
		[ObservableProperty]
		private string sourceText = string.Empty;

		/// <summary>
		/// Shows a report that is one image where it can be zoomed, and notes when it came from the server.
		/// </summary>
		/// <param name="Value">The rendered report.</param>
		partial void OnReportChanged(object? Value)
		{
			List<ZoomableImage> Images = [];
			FindImages(Value, Images);

			this.ReportImage = Images.Count == 1 ? Images[0].Source : null;
			this.SourceText = ServiceRef.Localizer[nameof(AppResources.ZoomFromServer), DateTime.Now.ToString("t", CultureInfo.CurrentCulture)];
		}

		/// <summary>
		/// Finds the images in a rendered report.
		/// </summary>
		/// <param name="View">A part of the report.</param>
		/// <param name="Found">The images found so far.</param>
		private static void FindImages(object? View, List<ZoomableImage> Found)
		{
			switch (View)
			{
				case ZoomableImage Image:
					Found.Add(Image);
					break;

				case Layout Layout:
					foreach (IView Child in Layout)
						FindImages(Child, Found);
					break;

				case ContentView Content:
					FindImages(Content.Content, Found);
					break;

				case Border Border:
					FindImages(Border.Content, Found);
					break;

				case ScrollView Scroll:
					FindImages(Scroll.Content, Found);
					break;
			}
		}

		/// <summary>
		/// Shares the report's image with another app.
		/// </summary>
		[RelayCommand]
		private Task ShareAsync()
		{
			return ImageShare.ShareAsync(this.ReportImage, this.Title ?? string.Empty);
		}

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
