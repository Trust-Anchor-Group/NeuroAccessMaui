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
		Task CheckUserBlockingAsync();
		Task<bool> CheckPasswordAndUnblockUserAsync(string password);
		Task<long> GetCurrentPasswordCounterAsync();

		// (Add other relevant authentication methods)
	}

}
