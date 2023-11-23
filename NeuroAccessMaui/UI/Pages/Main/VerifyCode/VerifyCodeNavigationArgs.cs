using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Main.VerifyCode;

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
	/// The page parent must support the code verification interface
	/// </summary>
	public ICodeVerification? CodeVerification { get; } = CodeVerification;

	/// <summary>
	/// The phone or email to display
	/// </summary>
	public string? PhoneOrEmail { get; } = PhoneOrEmail;

	/// <summary>
	/// Task completion source; can be used to wait for a result.
	/// </summary>
	public TaskCompletionSource<string?>? VarifyCode { get; internal set; } = new();
}
