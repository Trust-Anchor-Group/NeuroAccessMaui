using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Authentication
{
	[DefaultImplementation(typeof(AuthenticationService))]
	public interface IAuthenticationService : IDisposable, IAsyncDisposable
	{
		Task<string?> InputPasswordAsync(AuthenticationPurpose purpose);
		Task<bool> AuthenticateUserAsync(AuthenticationPurpose purpose, bool force = false);
		/// <summary>
		/// Checks if the user is blocked from authenticating after too many wrong PINs, and shows UI as needed.
		/// </summary>
		/// <returns>true if the user is blocked; otherwise false.</returns>
		Task<bool> CheckUserBlockingAsync();
		Task<bool> CheckPasswordAndUnblockUserAsync(string password);
		Task<long> GetCurrentPasswordCounterAsync();

		// (Add other relevant authentication methods)
	}

}
