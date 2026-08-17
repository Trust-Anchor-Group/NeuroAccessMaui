using System.Runtime.CompilerServices;
using Waher.Networking;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Network
{
	/// <summary>
	/// The network service is a wafer-thin wrapper around the <see cref="Connectivity"/> object.
	/// It exposes an event handler for monitoring connected state, and a DNS lookup method.
	/// It also has helper methods to make network requests and catch and display errors if they fail.
	/// </summary>
	[DefaultImplementation(typeof(NetworkService))]
	public interface INetworkService : ILoadableService
	{
		/// <summary>
		/// Triggers whenever network connectivity changes.
		/// </summary>
		event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

		/// <summary>
		/// Performs a DNS lookup for the specified domain name.
		/// </summary>
		/// <param name="domainName">The domain name whose name to resolve.</param>
		/// <returns>Host Name, TCP Port number, and if the host is an IP Address or not.</returns>
		Task<(string HostName, int Port, bool IsIpAddress)> LookupXmppHostnameAndPort(string domainName);

		/// <summary>
		/// Enables Neuron HTTP proxy for HTTP requests
		/// </summary>
		/// <param name="TokenSeconds">Number of seconds for which the authentication token should be valid.</param>
		/// <param name="CommunicationLayer">Optional communication layer for outputting error messages.</param>
		/// <param name="CancellationToken">Token used to cancel proxy setup.</param>
		/// <returns>A disposable scope that restores the previous web getter state.</returns>
		Task<IDisposable> EnableNeuronHttpProxyAsync(int TokenSeconds, ICommunicationLayer? CommunicationLayer, CancellationToken CancellationToken);

		/// <summary>
		/// Determines whether we have network (wifi/cellular/other) or not.
		/// </summary>
		bool IsOnline { get; }

		/// <summary>
		/// Tries to execute a arbitrary request. If there's an error this method catches it, logs it, and displays an alert to the user.
		/// </summary>
		/// <param name="func">The <c>Func</c> to execute.</param>
		/// <param name="rethrowException">Set to <c>true</c> if the exception should be rethrown, <c>false</c> otherwise.</param>
		/// <param name="displayAlert">Set to <c>true</c> if an alert should be displayed to the user, <c>false</c> otherwise.</param>
		/// <param name="memberName">(Optional) a method name to use.</param>
		/// <returns>If request was successful</returns>
		Task<bool> TryRequest(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");

		/// <summary>
		/// Tries to execute a arbitrary request. If there's an error this method catches it, logs it, and displays an alert to the user.
		/// </summary>
		/// <typeparam name="TReturn">The return type.</typeparam>
		/// <param name="func">The <c>Func</c> to execute.</param>
		/// <param name="rethrowException">Set to <c>true</c> if the exception should be rethrown, <c>false</c> otherwise.</param>
		/// <param name="displayAlert">Set to <c>true</c> if an alert should be displayed to the user, <c>false</c> otherwise.</param>
		/// <param name="memberName">(Optional) a method name to use.</param>
		/// <returns>If request was successful, and if so, the return value.</returns>
		Task<(bool Succeeded, TReturn? ReturnValue)> TryRequest<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
	}
}
