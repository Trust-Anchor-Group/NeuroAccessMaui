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

		private bool hasBeenToSettings = false;

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

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			App.AppActivated += this.App_OnActivated;

		}

		protected override Task OnDispose()
		{
			App.AppActivated -= this.App_OnActivated;

			return base.OnDispose();
		}

		protected async void App_OnActivated(object? sender, EventArgs e)
		{
			try
			{
				if (this.hasBeenToSettings)
				{
					await this.Close();
				}
			}
			catch (Exception)
			{
				//Ignore no need to handle this exception, the user needs to close popup manually
			}
		}

		[RelayCommand]
		private async Task Close()
		{
			await ServiceRef.UiService.PopAsync();
		}

		[RelayCommand]
		private void GoToSettings()
		{
			this.hasBeenToSettings = true;
			AppInfo.Current.ShowSettingsUI();
		}
	}
}
