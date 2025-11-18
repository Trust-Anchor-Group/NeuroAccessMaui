using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.OnboardingHelp
{
	public partial class OnboardingHelpViewModel : BasePopupViewModel
	{
		public OnboardingHelpViewModel()
		{
			this.SupportEmail = "neuro-access@trustanchorgroup.com";
		}

		[ObservableProperty]
		private string supportEmail;

		[RelayCommand]
		private async Task ContactSupport()
		{
			string Email = this.SupportEmail;
			string Subject = ServiceRef.Localizer[nameof(AppResources.SupportEmailSubject)];

			string MailtoUri = $"mailto:{Email}?subject={Uri.EscapeDataString(Subject)}";

			try
			{
				if (!await Launcher.OpenAsync(new Uri(MailtoUri)))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.EmailClientNotAvailable), this.SupportEmail],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception)
			{
				await ServiceRef.UiService.DisplayAlert(
					 ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					 ServiceRef.Localizer[nameof(AppResources.EmailClientNotAvailable), this.SupportEmail],
					 ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}
	}
}
