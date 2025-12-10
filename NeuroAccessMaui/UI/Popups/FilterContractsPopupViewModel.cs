using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class FilterContractsPopupViewModel : ReturningPopupViewModel<MyContractsViewModel.SelectableTag>
	{
		[ObservableProperty]
		private ObservableCollection<TextButton> buttons = new();

		public ObservableCollection<MyContractsViewModel.SelectableTag> Tags { get; }

		public FilterContractsPopupViewModel(ObservableCollection<MyContractsViewModel.SelectableTag> tags)
		{
			this.Tags = tags;

			foreach (var tag in tags)
			{
				this.Buttons.Add(new TextButton
				{
					LabelData = tag.Category,
					Command = new AsyncRelayCommand(async () => await this.SelectTagAsync(tag))
				});
			}
		}

		[RelayCommand]
		private static async Task Close()
		{
			await ServiceRef.PopupService.PopAsync();
		}

		private async Task SelectTagAsync(MyContractsViewModel.SelectableTag? tag)
		{
			this.TrySetResult(tag);
			await ServiceRef.PopupService.PopAsync();
		}
	}
}
