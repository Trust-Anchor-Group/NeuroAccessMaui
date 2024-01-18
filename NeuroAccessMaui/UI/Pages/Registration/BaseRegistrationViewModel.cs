using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration
{
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

		/// <summary>
		/// Override this method to do view model specific of setting the default properties values.
		/// </summary>
		public virtual Task DoAssignProperties()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this method to do view model specific of clearing properties which was previously filled.
		/// </summary>
		public virtual Task DoClearProperties()
		{
			return Task.CompletedTask;
		}
	}
}
