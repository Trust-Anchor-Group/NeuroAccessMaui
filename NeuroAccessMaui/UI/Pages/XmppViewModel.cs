using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// A view model that holds the XMPP state.
	/// </summary>
	public partial class XmppViewModel : BaseViewModel
	{
		/// <summary>
		/// Creates an instance of a <see cref="XmppViewModel"/>.
		/// </summary>
		protected XmppViewModel()
			: base()
		{
			this.StateSummaryText = ServiceRef.Localizer[nameof(AppResources.XmppState_Offline)];
			this.ConnectionStateText = ServiceRef.Localizer[nameof(AppResources.XmppState_Offline)];
			this.ConnectionStateColor = new SolidColorBrush(Colors.Red);
			this.StateSummaryText = string.Empty;
			this.SetConnectionStateAndText(ServiceRef.XmppService.State);
		}


		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			await base.OnDisposeAsync();
		}

		#region Properties

		/// <summary>
		/// Gets the current connection state as a user friendly localized string.
		/// </summary>
		[ObservableProperty]
		private string connectionStateText;

		/// <summary>
		/// Gets the current connection state as a color.
		/// </summary>
		[ObservableProperty]
		private Brush connectionStateColor;

		/// <summary>
		/// Gets the current state summary as a user friendly localized string.
		/// </summary>
		[ObservableProperty]
		private string stateSummaryText;

		/// <summary>
		/// Gets whether the view model is connected to an XMPP server.
		/// </summary>
		[ObservableProperty]
		private bool isConnected;

		#endregion

		/// <summary>
		/// Sets both the connection state and connection text to the appropriate value.
		/// </summary>
		/// <param name="State">The current state.</param>
		protected virtual void SetConnectionStateAndText(XmppState State)
		{
			this.ConnectionStateText = State.ToDisplayText();
			this.ConnectionStateColor = new SolidColorBrush(State.ToColor());
			this.IsConnected = State == XmppState.Connected;
			this.StateSummaryText = (ServiceRef.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
		}

		/// <summary>
		/// Listens to connection state changes from the XMPP server.
		/// </summary>
		/// <param name="_">The XMPP service instance.</param>
		/// <param name="NewState">New XMPP State.</param>
		protected virtual Task XmppService_ConnectionStateChanged(object? _, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.SetConnectionStateAndText(NewState);
			});
		}
	}
}
