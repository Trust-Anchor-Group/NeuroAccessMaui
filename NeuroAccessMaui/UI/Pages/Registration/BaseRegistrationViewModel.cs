using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.Pages.Registration;

/// <summary>
/// A base class for all view models that handle any part of the registration flow.
/// </summary>
/// <param name="Step">The current step for this instance.</param>
public abstract class BaseRegistrationViewModel(RegistrationStep Step) : BaseViewModel
{
	/// <summary>
	/// The current step for this view model instance.
	/// </summary>
	public RegistrationStep Step { get; } = Step;

	/*
    /// <summary>
    /// An event that fires whenever the <see cref="Step"/> property changes.
    /// </summary>
    public event EventHandler StepCompleted;

    #region Properties

    /// <summary>
    /// See <see cref="Title"/>
    /// </summary>
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(RegistrationStepViewModel), default(string));

    /// <summary>
    /// The title to display on the page or view rendering this view model.
    /// </summary>
    public string Title
    {
        get => (string)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

#endregion

/// <summary>
/// Call this method to fire the <see cref="StepCompleted"/> event.
/// </summary>
/// <param name="e"></param>
protected virtual void OnStepCompleted(EventArgs e)
    {
        this.StepCompleted?.Invoke(this, e);
    }
	*/

	/// <summary>
	/// Method called when view is appearing on the screen.
	/// </summary>
	///
	protected override async Task OnAppearing()
	{
		await base.OnAppearing();

		// Permits support during onboarding, before option is presented in main menu.
		ServiceRef.PlatformSpecific.ProhibitScreenCapture = false;
	}

	/// <summary>
	/// Override this method to do view model specific of setting the default properties values.
	/// </summary>
	public virtual Task DoAssignProperties()
	{
		return Task.CompletedTask;
	}
}
