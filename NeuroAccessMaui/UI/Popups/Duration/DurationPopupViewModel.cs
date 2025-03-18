using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Duration
{
	public partial class DurationPopupViewModel : BasePopupViewModel
	{
		[ObservableProperty]
		private ObservableCollection<TextButton> buttons;

		public DurationPopupViewModel(ObservableCollection<DurationUnits> AvailableUnits)
		{
			this.Buttons = new ObservableCollection<TextButton>();

			// Populate buttons based on the available units
			foreach (DurationUnits Unit in AvailableUnits)
			{
				this.Buttons.Add(new TextButton
				{
					LabelData = Unit.ToString(),
					Command = new RelayCommand(() => this.SelectUnit(Unit))
				});
			}
		}

		// Close command to dismiss the popup
		[RelayCommand]
		private static async Task Close()
		{
			await ServiceRef.UiService.PopAsync();
		}

		// Command that handles unit selection
		public void SelectUnit(DurationUnits unit)
		{
			// Perform any logic when a button is clicked
			Console.WriteLine($"Button clicked: {unit}");
		}
	}
}
