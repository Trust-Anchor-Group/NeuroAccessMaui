using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// View model for a popup that allows users to select a contract filter tag.
	/// Uses buttons bound to commands so tapping a button closes the popup and returns the selected tag.
	/// </summary>
	public partial class FilterContractsPopupViewModel : ReturningPopupViewModel<MyContractsViewModel.SelectableTag>
	{
		/// <summary>
		/// Collection of button models rendered by the popup. Each button represents a selectable tag.
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<TextButton> buttons = new();

		/// <summary>
		/// Original list of selectable tags provided by the caller.
		/// </summary>
		public ObservableCollection<MyContractsViewModel.SelectableTag> Tags { get; }

		/// <summary>
		/// Creates a new <see cref="FilterContractsPopupViewModel"/>.
		/// </summary>
		/// <param name="tags">Collection of tags to present for selection.</param>
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

		/// <summary>
		/// Closes the popup without returning a tag.
		/// </summary>
		[RelayCommand]
		private static async Task Close()
		{
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Sets the selected tag as the popup result and closes the popup.
		/// </summary>
		/// <param name="tag">Selected tag to return.</param>
		private async Task SelectTagAsync(MyContractsViewModel.SelectableTag? tag)
		{
			this.TrySetResult(tag);
			await ServiceRef.PopupService.PopAsync();
		}
	}
}
