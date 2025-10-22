using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Popups.Settings
{
	public partial class SelectLanguagePopup : BasePopup
	{

		public SelectLanguagePopup()
		{
			this.InitializeComponent();
			this.BindingContext = new SelectLanguagePopupViewModel();


			WeakReferenceMessenger.Default.Register<ScrollToLanguageMessage>(this, (r, m) =>
			{
				foreach (var element in this.LanguagesContainer.Children)
				{
					if (element is not VisualElement { BindingContext: ObservableLanguage lang })
						continue;

					if (lang.Language.Name != m.Value)
						continue;

					MainThread.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							await this.InnerScrollView.ScrollToAsync(element as Element, ScrollToPosition.MakeVisible, true);
						}
						catch { /* ignore */ }
					});

					break;
				}
			});
		}
		public override Task OnDisappearingAsync()
		{
			WeakReferenceMessenger.Default.Unregister<ScrollToLanguageMessage>(this);
			return base.OnDisappearingAsync();
		}
	}
}
