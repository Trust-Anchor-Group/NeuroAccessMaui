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

		/// <summary>
		/// Gets whether a forgotten PIN can be reset by verifying the user with device biometrics.
		/// </summary>
		/// <remarks>
		/// This only depends on the device supporting biometrics, not on the user having enabled biometric login in the app.
		/// </remarks>
		bool CanResetPinWithBiometrics { get; }

		/// <summary>
		/// Resets a forgotten PIN: confirms with the user, verifies them with device biometrics, clears failed PIN
		/// attempts and lockout, closes open popups and takes the user to define a new PIN. The user's identity is kept.
		/// </summary>
		/// <returns>true if the user was verified and is being taken to define a new PIN; otherwise false.</returns>
		Task<bool> ResetForgottenPinAsync();

		// (Add other relevant authentication methods)
	}

}
