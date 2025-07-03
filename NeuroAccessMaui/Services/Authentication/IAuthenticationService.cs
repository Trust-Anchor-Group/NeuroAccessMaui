using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Authentication
{
	public interface IAuthenticationService
	{
		Task<string?> InputPasswordAsync(AuthenticationPurpose purpose);
		Task<bool> AuthenticateUserAsync(AuthenticationPurpose purpose, bool force = false);
		Task CheckUserBlockingAsync();
		Task<bool> CheckPasswordAndUnblockUserAsync(string password);
		// (Add other relevant authentication methods)
	}

}
