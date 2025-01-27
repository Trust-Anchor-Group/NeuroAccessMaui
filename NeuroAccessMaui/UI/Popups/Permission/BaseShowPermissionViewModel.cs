using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Permission
{
	public partial class BaseShowPermissionViewModel : BasePopupViewModel
	{
		private string title = "";

		public string Title
		{
			get => this.title;
			set
			{
				if (value == this.title) return;
				this.title = value;
				this.OnPropertyChanged();
			}
		}

		private string text = "";

		public string Text
		{
			get => this.text;
			set
			{
				if (value == this.text) return;
				this.text = value;
				this.OnPropertyChanged();
			}
		}

		public BaseShowPermissionViewModel(string title, string text)
		{
			this.Title = title;
			this.Text = text;
		}

		public BaseShowPermissionViewModel() { }

		[RelayCommand]
		public void Close()
		{
			ServiceRef.UiService.PopAsync();
		}

		[RelayCommand]
		public void GoToSettings()
		{
			ServiceRef.UiService.PopAsync();
			AppInfo.Current.ShowSettingsUI();
		}
	}
}
