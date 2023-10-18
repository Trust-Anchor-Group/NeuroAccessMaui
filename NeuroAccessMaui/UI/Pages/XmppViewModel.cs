using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages;

/// <summary>
/// A view model that holds the XMPP state.
/// </summary>
public partial class XmppViewModel : BaseViewModel
{
	/// <summary>
	/// Creates an instance of a <see cref="XmppViewModel"/>.
	/// </summary>
	protected XmppViewModel()
	{
		this.StateSummaryText = ServiceRef.Localizer[nameof(AppResources.XmppState_Offline)];
		this.ConnectionStateText = ServiceRef.Localizer[nameof(AppResources.XmppState_Offline)];
		this.ConnectionStateColor = new SolidColorBrush(Colors.Red);
		this.StateSummaryText = string.Empty;
	}

	/// <inheritdoc/>
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		this.SetConnectionStateAndText(ServiceRef.XmppService.State);
		ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
	}

	/// <inheritdoc/>
	protected override async Task OnDispose()
	{
		ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
		await base.OnDispose();
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
	/// <param name="state">The current state.</param>
	protected virtual void SetConnectionStateAndText(XmppState state)
	{
		this.ConnectionStateText = state.ToDisplayText();
		this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
		this.IsConnected = state == XmppState.Connected;
		this.StateSummaryText = (ServiceRef.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
	}

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
}
