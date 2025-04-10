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
					Command = new RelayCommand(() => {
						this.SelectUnit(Unit);
					})
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
		public void SelectUnit(DurationUnits Unit)
		{
			this.result.TrySetResult(Unit);

			this.CloseCommand.Execute(null);
		}

		public override Task OnPop()
		{
			try
			{
				if (!this.result.Task.IsCompleted)
					this.result.TrySetResult(null);
			}
			catch (Exception)
			{
				// ignored
			}

			return base.OnPop();
		}
	}
}
