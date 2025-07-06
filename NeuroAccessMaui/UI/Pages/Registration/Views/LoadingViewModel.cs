using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class LoadingViewModel : BaseRegistrationViewModel
	{
		public LoadingViewModel()
			: base(RegistrationStep.Complete)
		{
		}

		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			try
			{
				if (App.Current is not null)
					await App.Current.InitCompleted;

			}
			catch (Exception)
			{
			}
			finally
			{
				GoToRegistrationStep(ServiceRef.TagProfile.Step);
			}
		}
/*
		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			try
			{
				if (App.Current is not null)
					await App.Current.InitCompleted;

				//Load the theme for the provider, if it exists.
				await ServiceRef.ThemeService.ApplyProviderTheme();
			}
			catch (Exception)
			{
			}
			finally
			{
				GoToRegistrationStep(ServiceRef.TagProfile.Step);
			}

			//	this.IsBusy = true;
			//	ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
			//	ServiceRef.XmppService.Loaded += this.XmppService_Loaded;
		}
*/
		/// <inheritdoc />
		public override async Task OnDisposeAsync()
		{
		//	ServiceRef.XmppService.Loaded -= this.XmppService_Loaded;
		//	ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			await base.OnDisposeAsync();
		}

		#region Properties

		/// <summary>
		/// Gets the current connection state as a user friendly localized string.
		/// </summary>
		[ObservableProperty]
		private string? connectionStateText;

		/// <summary>
		/// Gets the current connection state as a color.
		/// </summary>
		[ObservableProperty]
		private Brush? connectionStateColor;

		/// <summary>
		/// Gets the current state summary as a user friendly localized string.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(DisplayConnectionText))]
		private string? stateSummaryText;

		/// <summary>
		/// Gets whether the view model is connected to an XMPP server.
		/// </summary>
		//[ObservableProperty]
		//private bool isConnected;

		public bool DisplayConnectionText => ServiceRef.TagProfile.IsCompleteOrWaitingForValidation() && !string.IsNullOrEmpty(this.ConnectionStateText);

		#endregion

		/// <summary>
		/// Listens to connection state changes from the XMPP server.
		/// </summary>
		/// <param name="_">The XMPP service instance.</param>
		/// <param name="NewState">New XMPP State.</param>
		protected virtual Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (MainThread.IsMainThread)
			{
				this.SetConnectionStateAndText(NewState);
				return Task.CompletedTask;
			}
			else
			{
				return MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.SetConnectionStateAndText(NewState);
				});
			}
		}

		/// <inheritdoc/>
		protected void SetConnectionStateAndText(XmppState state)
		{
			this.ConnectionStateText = state.ToDisplayText();
			//this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
			//this.IsConnected = state == XmppState.Connected;
			this.StateSummaryText = (ServiceRef.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
		}

		private void XmppService_Loaded(object? Sender, LoadedEventArgs e)
		{
			try
			{
				if (e.IsLoaded && !e.IsResuming)
				{
					this.IsBusy = false;

					// XmppService_Loaded method might be called from OnInitialize method.
					// We cannot update the main page while some initialization is still running (well, we can technically but there will be chaos).
					// Therefore, do not await this method and do not call it synchronously, even if we are already on the main thread.
					Task ExecutionTask = Task.Run(() =>
					{
						// The right step will be loaded by TagProfile service,
						// but we may choose to change it here if necessary

						// GoToRegistrationStep(RegistrationStep.<TheOtherState>);
						GoToRegistrationStep(ServiceRef.TagProfile.Step);
					});
				}
			}
			catch (Exception Exception)
			{
				ServiceRef.LogService?.LogException(Exception);
			}
		}
	}
}
