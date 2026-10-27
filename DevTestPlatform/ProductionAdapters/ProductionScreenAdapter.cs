using CommunityToolkit.Mvvm.Input;
using DevTestPlatform.Catalog;
using DevTestPlatform.Rendering;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;
using NeuroAccessMaui.UI.Pages.Onboarding.Views;

namespace DevTestPlatform.ProductionAdapters;

/// <summary>Constructs current production views with their current strongly typed ViewModels.</summary>
public sealed class ProductionScreenAdapter
{
    /// <summary>Creates one independently owned preview from current production code.</summary>
    /// <param name="Screen">The selected production render subject.</param>
    /// <param name="Scenario">Its controlled visual state.</param>
    /// <returns>A new rendering session.</returns>
    public RenderSession Create(ScreenDefinition Screen, ScenarioDefinition Scenario)
    {
        switch (Screen.Key)
        {
            case "onboarding.provider":
                WelcomeOnboardingStepViewModel Welcome = new WelcomeOnboardingStepViewModel { IsLoading = false };
                return new RenderSession(new WelcomeStepView(Welcome), Welcome);
            case "onboarding.phone":
                ValidatePhoneOnboardingStepViewModel Phone = new ValidatePhoneOnboardingStepViewModel
                {
                    SelectedCountry = ISO_3166_1.Countries.First(Country => Country.Alpha2 == (Scenario.Key == "country-se" ? "SE" : "US")),
                    PhoneNumber = Scenario.Value,
                    IsBusy = Scenario.IsBusy,
                    IsPhoneReadOnly = Scenario.Key == "read-only"
                };
                return new RenderSession(new ValidatePhoneStepView(Phone), Phone);
            case "onboarding.email":
                ValidateEmailOnboardingStepViewModel Email = new ValidateEmailOnboardingStepViewModel { EmailText = Scenario.Value, IsBusy = Scenario.IsBusy };
                return new RenderSession(new ValidateEmailStepView(Email), Email);
            case "onboarding.username":
                NameEntryOnboardingStepViewModel Username = new NameEntryOnboardingStepViewModel { Username = Scenario.Value };
                return new RenderSession(new NameEntryStepView(Username), Username);
            case "onboarding.account":
                CreateAccountOnboardingStepViewModel Account = new CreateAccountOnboardingStepViewModel { IsBusy = true };
                return new RenderSession(new CreateAccountStepView(Account), Account);
            case "onboarding.pin":
                DefinePasswordOnboardingStepViewModel Password = new DefinePasswordOnboardingStepViewModel
                {
                    PasswordText1 = Scenario.Value,
                    PasswordText2 = Scenario.Key == "mismatch" ? "258048" : Scenario.Value
                };
                return new RenderSession(new DefinePasswordStepView(Password), Password);
            case "onboarding.biometrics":
                BiometricsOnboardingStepViewModel Biometrics = new BiometricsOnboardingStepViewModel();
                return new RenderSession(new BiometricsStepView(Biometrics), Biometrics);
            case "onboarding.finalize":
                FinalizeOnboardingStepViewModel Finalize = new FinalizeOnboardingStepViewModel();
                return new RenderSession(new FinalizeStepView(Finalize), Finalize);
            case "verification.phone-code":
            case "verification.email-code":
                string Destination = Screen.Key == "verification.phone-code" ? "+1 5551234" : "alex@example.com";
                VerifyCodeViewModel Verification = new VerifyCodeViewModel(new VerifyCodeNavigationArgs(null, Destination)) { IsBusy = Scenario.IsBusy };
                VerifyCodePage VerificationView = VerifyCodePage.CreateWithViewModel(Verification);
                return new RenderSession(VerificationView, Verification, () => Verification.VerifyCodeText = Scenario.Value);
            case "components.text-button":
                TextButton Button = new TextButton
                {
                    LabelData = AppResources.SendCode,
                    IsEnabled = Scenario.Key != "disabled" && !Scenario.IsBusy,
                    Command = new RelayCommand(() => { }),
                    AutomationId = NeuroAccessMaui.UI.Automation.AutomationIds.DevButtonComponentsTextbuttonSample
                };
                Button.SetDynamicResource(VisualElement.StyleProperty, "FilledTextButton");
                return new RenderSession(Button);
            case "components.composite-entry":
                CompositeEntry Entry = new CompositeEntry
                {
                    EntryData = Scenario.Value,
                    IsEnabled = Scenario.Key != "disabled",
                    IsValid = Scenario.Key != "invalid",
                    Keyboard = Keyboard.Telephone,
                    AutomationId = NeuroAccessMaui.UI.Automation.AutomationIds.DevInputComponentsCompositeentrySample
                };
                Entry.SetDynamicResource(VisualElement.StyleProperty, "RegularCompositeEntry");
                return new RenderSession(Entry, AfterLoaded: Scenario.Key == "focused" ? () => Entry.Focus() : null);
            default:
                throw new InvalidOperationException($"No current-production adapter for {Screen.Key}.");
        }
    }
}
