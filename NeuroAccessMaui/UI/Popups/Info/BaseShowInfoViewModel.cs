using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Info
{
	public partial class BaseShowInfoViewModel : BasePopupViewModel
	{
		private string infoTitle = "";

		public string InfoTitle
		{
			get => this.infoTitle;
			set
			{
				if (value == this.infoTitle) return;
				this.infoTitle = value;
				this.OnPropertyChanged();
			}
		}

		private string infoText = "";

		public string InfoText
		{
			get => this.infoText;
			set
			{
				if (value == this.infoText) return;
				this.infoText = value;
				this.OnPropertyChanged();
			}
		}

		public BaseShowInfoViewModel(string infoTitle, string infoText)
		{
			this.InfoTitle = infoTitle;
			this.InfoText = infoText;
		}

		public BaseShowInfoViewModel() { }

		[RelayCommand]
		public void Close()
		{
			ServiceRef.PopupService.PopAsync();
		}
	}
}
