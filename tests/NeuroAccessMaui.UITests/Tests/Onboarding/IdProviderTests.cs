using NeuroAccessMaui.UITests.Screens;

namespace NeuroAccessMaui.UITests.Tests.Onboarding;

/// <summary>Preserves provider-selection navigation and phone-page assertions.</summary>
[TestClass, TestCategory("Smoke")]
public sealed class IdProviderTests : BaseUiTest
{
	/// <summary>Chooses the suggested provider and verifies the phone-verification form.</summary>
	[TestMethod]
	public void Test_01_SelectForMeNavigatesToPhoneVerification()
	{
		UiSession Session = this.Open();
		OnboardingScreen Screen = new OnboardingScreen(Session);
		Screen.AssertProvider();
		Screen.SelectProvider();
		Screen.AssertPhone();
	}
}
