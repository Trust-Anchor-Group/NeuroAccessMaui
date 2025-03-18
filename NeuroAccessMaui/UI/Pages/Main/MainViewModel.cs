using System.ComponentModel;
using System.Diagnostics.Contracts;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainViewModel : QrXmppViewModel
	{
		public ObservableTask<int, int> ObservableTask { get; } = new();


		public MainViewModel()
			: base()
		{
			this.ObservableTask.PropertyChanged += ((sender, args) =>
			{
				if (args.PropertyName == nameof(this.ObservableTask.State))
				{
					Console.WriteLine(this.ObservableTask.State);
				}
			});
		}
		public bool IsNotStarted => this.ObservableTask.IsNotStarted;

		[RelayCommand]
        private async Task Start()
        {
            // Use the Load method of TaskNotifier to start an asynchronous operation.
            this.ObservableTask.Load(this.Foo, this.StartCommand);
        }

		private async Task<int> Foo(TaskContext<int> Context)
		{
			int Sum = 0;
			// Simulate work by summing numbers 1 to 10 with a delay.
			for (int i = 1; i <= 100; i++)
			{
				// Support cancellation.
				await Task.Delay(50, Context.CancellationToken);
				Sum += i;
				// Report progress (for example, as a percentage).
				Context.Progress.Report(i * 1);
				if (i == 50)
					await ServiceRef.XmppService.GetContract("Test");
			}
			return Sum;
		}

		[ObservableTaskCommand(ObservableTaskCommandOptions.AllowConcurrentRestart)]
		private async Task<int> Bar(TaskContext<int> Context)
		{
			int Sum = 0;
			// Simulate work by summing numbers 1 to 10 with a delay.
			for (int i = 1; i <= 100; i++)
			{
				// Support cancellation.
				await Task.Delay(50, Context.CancellationToken);
				Sum += i;
				// Report progress (for example, as a percentage).
				Context.Progress.Report(i * 1);
				if (i == 50)
					await ServiceRef.XmppService.GetContract("Test");
			}
			return Sum;
		}


		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity));



		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			await this.OnIsConnectedChanged(); // Call this method in case the connection state has already changed before the view model was initialized.
		}

		protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					await this.OnIsConnectedChanged();
					break;
			}
		}

		private async Task OnIsConnectedChanged()
		{
			try
			{
				if (this.IsConnected && ServiceRef.TagProfile.LegalIdentityNeedsRefreshing())
				{
					LegalIdentity RefreshedIdentity = await ServiceRef.XmppService.GetLegalIdentity(ServiceRef.TagProfile.LegalIdentity?.Id);
					await MainThread.InvokeOnMainThreadAsync(async () => await ServiceRef.TagProfile.SetLegalIdentity(RefreshedIdentity, false));
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.ScanQrCodeCommand.NotifyCanExecuteChanged();
			}
		}

		public bool CanScanQrCode => this.IsConnected;

		[RelayCommand(CanExecute = nameof(CanScanQrCode))]
		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task ViewId()
		{
			try
			{
				if(await App.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
