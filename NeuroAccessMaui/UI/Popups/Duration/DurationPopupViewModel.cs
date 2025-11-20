using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Duration
{
	public partial class DurationPopupViewModel : ReturningPopupViewModel<DurationUnits?>
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
					LabelData = ServiceRef.Localizer[Unit.ToString() + "Capitalized"],
					Command = new AsyncRelayCommand(async () => await this.SelectUnitAsync(Unit))
				});
			}
		}

		// Close command to dismiss the popup
		[RelayCommand]
		private static async Task Close()
		{
			await ServiceRef.PopupService.PopAsync();
		}

		// Command that handles unit selection
		private async Task SelectUnitAsync(DurationUnits unit)
		{
			this.TrySetResult(unit);
			await ServiceRef.PopupService.PopAsync();
		}

		protected override Task OnPopAsync()
		{
			if (!this.Result.IsCompleted)
			{
				this.TrySetResult(null);
			}
			return base.OnPopAsync();
		}
	}
}
