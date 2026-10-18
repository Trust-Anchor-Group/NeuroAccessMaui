using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Automation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Onboarding;

/// <summary>Observes the real profile on existing onboarding chrome in automation builds only.</summary>
public partial class OnboardingPage
{
	partial void ConfigureAutomation()
	{
		this.Loaded += this.OnAutomationLoaded;
		this.Unloaded += this.OnAutomationUnloaded;
	}

	private void OnAutomationLoaded(object? Sender, EventArgs E)
	{
		ServiceRef.TagProfile.OnPropertiesChanged -= this.OnAutomationProfileChanged;
		ServiceRef.TagProfile.OnPropertiesChanged += this.OnAutomationProfileChanged;
		this.OnAutomationProfileChanged(Sender, E);
	}

	private void OnAutomationUnloaded(object? Sender, EventArgs E) =>
		ServiceRef.TagProfile.OnPropertiesChanged -= this.OnAutomationProfileChanged;

	private void OnAutomationProfileChanged(object? Sender, EventArgs E)
	{
		this.Dispatcher.Dispatch(() =>
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			// Bindable metadata changes with the profile; the CLR AutomationId setter only permits one assignment.
			this.GridWithAnimation.SetValue(AutomationIdProperty, Identity is null ? null
				: AutomationIds.ForKey(AutomationIds.ControlOnboardingProfileIdentity, Identity.State.ToString(), Identity.Id));
		});
	}
}
