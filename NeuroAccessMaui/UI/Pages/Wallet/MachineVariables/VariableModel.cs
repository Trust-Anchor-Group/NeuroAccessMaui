using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Content;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Represents a state-machine variable.
	/// </summary>
	/// <param name="Name">Name of variable</param>
	/// <param name="Value">Value of variable</param>
	public partial class VariableModel(string Name, object Value) : BaseViewModel
	{
		private readonly string name = Name;

		/// <summary>
		/// Name of variable
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// The name in words, such as "Amount bought" for "AmountBought".
		/// </summary>
		public string DisplayName => TokenText.ToWords(this.name);

		/// <summary>
		/// Value of variable
		/// </summary>
		[ObservableProperty]
		private object value = Value;

		/// <summary>
		/// Value as script
		/// </summary>
		[ObservableProperty]
		private string asScript = Expression.ToExpressionString(Value);

		/// <summary>
		/// The value in the user's format: numbers and dates as the phone shows them, yes or no, and texts without quotes.
		/// </summary>
		[ObservableProperty]
		private string displayValue = FormatValue(Value);

		/// <summary>
		/// Updates the value of the variable.
		/// </summary>
		/// <param name="Value">Value of variable</param>
		public void UpdateValue(object Value)
		{
			this.Value = Value;
			this.AsScript = Expression.ToExpressionString(Value);
			this.DisplayValue = FormatValue(Value);
		}

		/// <summary>
		/// Formats a value for the user.
		/// </summary>
		/// <param name="Value">The value.</param>
		/// <returns>The value as text.</returns>
		public static string FormatValue(object? Value)
		{
			CultureInfo Culture = CultureInfo.CurrentCulture;

			return Value switch
			{
				null => "-",
				string Text => string.IsNullOrEmpty(Text) ? "-" : Text,
				bool Boolean => ServiceRef.Localizer[Boolean ? nameof(AppResources.Yes) : nameof(AppResources.No)],
				decimal Number => ObservableParameter.FormatNumber(Number),
				double Number => Number.ToString("#,0.##########", Culture),
				int Number => Number.ToString("N0", Culture),
				long Number => Number.ToString("N0", Culture),
				DateTime Time => (Time.Kind == DateTimeKind.Utc ? Time.ToLocalTime() : Time).ToString(Time.TimeOfDay == TimeSpan.Zero ? "d" : "g", Culture),
				TimeSpan Span => ObservableParameter.FormatTimeSpan(Span),
				Duration Length => DurationToString.ToString(Length),
				_ => Expression.ToExpressionString(Value)
			};
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
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		#endregion
	}
}
