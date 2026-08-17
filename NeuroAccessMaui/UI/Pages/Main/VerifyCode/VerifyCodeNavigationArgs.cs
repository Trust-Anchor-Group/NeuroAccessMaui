using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Main.VerifyCode
{
	public interface ICodeVerification
	{
		int CountDownSeconds { get; set; }
		IDispatcherTimer CountDownTimer { get; }
		IAsyncRelayCommand ResendCodeCommand { get; }
	}

	/// <summary>
	/// Holds navigation parameters specific to VerifyCode view
	/// </summary>
	public class VerifyCodeNavigationArgs(ICodeVerification? CodeVerification = null, string? PhoneOrEmail = null) : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="VerifyCodeNavigationArgs"/> class.
		/// </summary>
		public VerifyCodeNavigationArgs() : this(null) { }

		/// <summary>
		/// Creates navigation arguments that automatically obtain and submit a verification code.
		/// </summary>
		/// <param name="CodeVerification">The code verification workflow.</param>
		/// <param name="PhoneOrEmail">The phone number or email address being verified.</param>
		/// <param name="AutoVerifyCode">The function that obtains the verification code.</param>
		/// <returns>The configured navigation arguments.</returns>
		public static VerifyCodeNavigationArgs CreateAutoVerify(ICodeVerification? CodeVerification, string? PhoneOrEmail,
			Func<Task<string?>> AutoVerifyCode)
		{
			return new VerifyCodeNavigationArgs(CodeVerification, PhoneOrEmail)
			{
				AutoVerifyCode = AutoVerifyCode
			};
		}

		/// <summary>
		/// The page parent must support the code verification interface
		/// </summary>
		public ICodeVerification? CodeVerification { get; } = CodeVerification;

		/// <summary>
		/// The phone or email to display
		/// </summary>
		public string? PhoneOrEmail { get; } = PhoneOrEmail;

		/// <summary>
		/// Gets the optional function used to obtain a verification code automatically.
		/// </summary>
		public Func<Task<string?>>? AutoVerifyCode { get; init; }

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<string?>? VarifyCode { get; internal set; } = new();
	}
}
