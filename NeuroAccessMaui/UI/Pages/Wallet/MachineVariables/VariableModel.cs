using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Represents a state-machine variable.
	/// </summary>
	public partial class VariableModel : BaseViewModel
	{
		private readonly string name;

		/// <summary>
		/// Represents a state-machine variable.
		/// </summary>
		/// <param name="Name">Name of variable</param>
		/// <param name="Value">Value of variable</param>
		public VariableModel(string Name, object Value)
		{
			this.name = Name;
			this.value = Value;
			this.asScript = Expression.ToString(Value);
		}

		/// <summary>
		/// Name of variable
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// Value of variable
		/// </summary>
		[ObservableProperty]
		private object value;

		/// <summary>
		/// Value as script
		/// </summary>
		[ObservableProperty]
		private string asScript;

		/// <summary>
		/// Updates the value of the variable.
		/// </summary>
		/// <param name="Value">Value of variable</param>
		public void UpdateValue(object Value)
		{
			this.Value = Value;
			this.AsScript = Expression.ToString(Value);
		}

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		[RelayCommand]
		private async Task CopyToClipboard()
		{
			try
			{
				await Clipboard.SetTextAsync(this.AsScript);
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		#endregion
	}
}
