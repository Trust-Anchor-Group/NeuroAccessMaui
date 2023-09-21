using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using NeuroAccessMaui.Resources.Languages;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Network;

[Singleton]
internal class NetworkService : LoadableService, INetworkService
{
	private const int defaultXmppPortNumber = 5222;

	public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

	public NetworkService()
	{
	}

	///<inheritdoc/>
	public override Task Load(bool isResuming, CancellationToken cancellationToken)
	{
		if (this.BeginLoad(cancellationToken))
		{
			if (DeviceInfo.Platform != DevicePlatform.Unknown && !DesignMode.IsDesignModeEnabled) // Need to check this, as Xamarin.Essentials doesn't work in unit tests. It has no effect when running on a real phone.
			{
				Connectivity.ConnectivityChanged += this.Connectivity_ConnectivityChanged;
			}

			this.EndLoad(true);
		}

		return Task.CompletedTask;
	}

	///<inheritdoc/>
	public override Task Unload()
	{
		if (this.BeginUnload())
		{
			if (DeviceInfo.Platform != DevicePlatform.Unknown)
			{
				Connectivity.ConnectivityChanged -= this.Connectivity_ConnectivityChanged;
			}

			this.EndUnload();
		}

		return Task.CompletedTask;
	}

	private void Connectivity_ConnectivityChanged(object Sender, ConnectivityChangedEventArgs e)
	{
		this.ConnectivityChanged?.Invoke(this, e);
	}

	public virtual bool IsOnline =>
		Connectivity.NetworkAccess == NetworkAccess.Internet ||
		Connectivity.NetworkAccess == NetworkAccess.ConstrainedInternet;

	public async Task<(string hostName, int port, bool isIpAddress)> LookupXmppHostnameAndPort(string domainName)
	{
		if (IPAddress.TryParse(domainName, out IPAddress _))
		{
			return (domainName, defaultXmppPortNumber, true);
		}

		try
		{
			SRV endpoint = await DnsResolver.LookupServiceEndpoint(domainName, "xmpp-client", "tcp");

			if (endpoint is not null && !string.IsNullOrWhiteSpace(endpoint.TargetHost) && endpoint.Port > 0)
			{
				return (endpoint.TargetHost, endpoint.Port, false);
			}
		}
		catch (Exception)
		{
			// No service endpoint registered
		}

		return (domainName, defaultXmppPortNumber, false);
	}

	public async Task<bool> TryRequest(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
	{
		(bool succeeded, bool _) = await this.PerformRequestInner(async () =>
		{
			await func();
			return true;
		}, memberName, rethrowException, displayAlert);

		return succeeded;
	}

	public Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
	{
		return this.PerformRequestInner(async () => await func(), memberName, rethrowException, displayAlert);
	}

	private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(Func<Task<TReturn>> func, string memberName, bool rethrowException = false, bool displayAlert = true)
	{
		Exception thrownException;
		try
		{
			if (!this.IsOnline)
			{
				thrownException = new MissingNetworkException(ServiceRef.Localizer[nameof(AppResources.ThereIsNoNetwork)]);
				ServiceRef.LogService.LogException(thrownException, GetParameter(memberName));

				if (displayAlert)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						CreateMessage(ServiceRef.Localizer[nameof(AppResources.ThereIsNoNetwork)], memberName));
				}
			}
			else
			{
				TReturn t = await func().TimeoutAfter(Constants.Timeouts.GenericRequest);
				return (true, t);
			}
		}
		catch (AggregateException ae)
		{
			thrownException = ae;

			if (ae.InnerException is TimeoutException te)
			{
				ServiceRef.LogService.LogException(te, GetParameter(memberName));

				if (displayAlert)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						CreateMessage(ServiceRef.Localizer[nameof(AppResources.RequestTimedOut)], memberName));
				}
			}
			else if (ae.InnerException is TaskCanceledException tce)
			{
				ServiceRef.LogService.LogException(tce, GetParameter(memberName));

				if (displayAlert)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						CreateMessage(ServiceRef.Localizer[nameof(AppResources.RequestWasCancelled)], memberName));
				}
			}
			else if (ae.InnerException is not null)
			{
				ServiceRef.LogService.LogException(ae.InnerException, GetParameter(memberName));

				if (displayAlert)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						CreateMessage(ae.InnerException.Message, memberName));
				}
			}
			else
			{
				ServiceRef.LogService.LogException(ae, GetParameter(memberName));

				if (displayAlert)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						CreateMessage(ae.Message, memberName));
				}
			}
		}
		catch (TimeoutException te)
		{
			thrownException = te;
			ServiceRef.LogService.LogException(te, GetParameter(memberName));

			if (displayAlert)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					CreateMessage(ServiceRef.Localizer[nameof(AppResources.RequestTimedOut)], memberName));
			}
		}
		catch (TaskCanceledException tce)
		{
			thrownException = tce;
			ServiceRef.LogService.LogException(tce, GetParameter(memberName));

			if (displayAlert)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					CreateMessage(ServiceRef.Localizer[nameof(AppResources.RequestWasCancelled)], memberName));
			}
		}
		catch (Exception e)
		{
			string message;

			thrownException = e;

			if (e is XmppException xe && xe.Stanza is not null)
			{
				message = xe.Stanza.InnerText;
			}
			else
			{
				message = e.Message;
			}

			ServiceRef.LogService.LogException(e, GetParameter(memberName));

			if (displayAlert)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					CreateMessage(message, memberName));
			}
		}

		if (rethrowException)
		{
			ExceptionDispatchInfo.Capture(thrownException).Throw();
		}

		return (false, default);
	}


	private static string CreateMessage(string message, string memberName)
	{
#if DEBUG
		if (!string.IsNullOrWhiteSpace(memberName))
		{
			return message + Environment.NewLine + "Caller: " + memberName;
		}
#endif
		return message;
	}

	private static KeyValuePair<string, object>[] GetParameter(string memberName)
	{
		if (!string.IsNullOrWhiteSpace(memberName))
		{
			return new[]
			{
				new KeyValuePair<string, object>("Caller", memberName)
			};
		}

		return new KeyValuePair<string, object>[0];
	}
}
