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
		#region Private Properties

		private string title = "";
		private string description = "";
		private string descriptionSecondary = "";

		#endregion
		#region Public Properties

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

		public string Description
		{
			get => this.description;
			set
			{
				if (value == this.description) return;
				this.description = value;
				this.OnPropertyChanged();
			}
		}

		public string DescriptionSecondary
		{
			get => this.descriptionSecondary;
			set
			{
				if (value == this.descriptionSecondary) return;
				this.descriptionSecondary = value;
				this.OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets the size of the background for Camera Icon 
		/// </summary>
		public double CameraIconBackgroundSize => 120.0;

		/// <summary>
		/// Gets the Readius of the background for Camera Icon
		/// </summary>
		public double CameraIconBackgroundCornerRadius => this.CameraIconBackgroundSize / 2;

		/// <summary>
		/// Gets the size of the Camera Icon
		/// </summary>
		public double CameraIconSize => 60.0;

		#endregion

		public BaseShowPermissionViewModel(string title, string description, string descriptionSecondary)
		{
			this.Title = title;
			this.Description = description;
			this.DescriptionSecondary = descriptionSecondary;
		}

		public BaseShowPermissionViewModel(string title, string description)
		{
			this.Title = title;
			this.Description = description;
			this.DescriptionSecondary = "";
		}

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
