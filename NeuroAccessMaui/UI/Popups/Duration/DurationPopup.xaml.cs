using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Duration
{
	public partial class DurationPopup
	{
		public DurationPopup(ObservableCollection<DurationUnits> AvailableUnits)
		{
			this.InitializeComponent();
			DurationPopupViewModel ViewModel = new DurationPopupViewModel(AvailableUnits);
			this.BindingContext = ViewModel;
		}
	}
}
