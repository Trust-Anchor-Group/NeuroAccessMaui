using System.Collections;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Content;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Represents a state-machine variable. When its value changes while it is shown, it is marked as changed for a moment,
	/// with the value it had before.
	/// </summary>
	/// <param name="Name">Name of variable</param>
	/// <param name="Value">Value of variable</param>
	public partial class VariableModel(string Name, object Value) : BaseViewModel
	{
		/// <summary>
		/// How long a changed value stays marked.
		/// </summary>
		private static readonly TimeSpan changeHighlightTime = TimeSpan.FromSeconds(4);

		private readonly string name = Name;
		private string unit = string.Empty;
		private int changeVersion;

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
		[NotifyPropertyChangedFor(nameof(IsList))]
		private object value = Value;

		/// <summary>
		/// Value as script
		/// </summary>
		[ObservableProperty]
		private string asScript = Expression.ToExpressionString(Value);

		/// <summary>
		/// The value in the user's format: numbers and dates as the phone shows them, yes or no, and texts without quotes.
		/// A number keeps its currency, when it is known.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private string displayValue = FormatValue(Value);

		/// <summary>
		/// If the value changed a moment ago.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private bool isChanged;

		/// <summary>
		/// The value before it changed, as shown to the user, while the value is marked as changed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PreviousValueText))]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private string? previousValue;

		/// <summary>
		/// The value before it changed, such as "was 12", while the value is marked as changed.
		/// </summary>
		public string? PreviousValueText => this.PreviousValue is null
			? null
			: ServiceRef.Localizer[nameof(AppResources.ValueWas), this.PreviousValue].Value;

		/// <summary>
		/// If the value is a list or a set of named values, which opens on its own page.
		/// </summary>
		public bool IsList => this.Value is IEnumerable and not string;

		/// <summary>
		/// What screen readers say about the value: the name, the value, and the value before when it changed a moment ago.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				string Result = this.DisplayName + ", " + this.DisplayValue;

				if (this.IsChanged && !string.IsNullOrEmpty(this.PreviousValueText))
					Result += ", " + this.PreviousValueText;

				return Result;
			}
		}

		/// <summary>
		/// Updates the value of the variable.
		/// </summary>
		/// <param name="Value">Value of variable</param>
		public void UpdateValue(object Value)
		{
			this.Value = Value;
			this.AsScript = Expression.ToExpressionString(Value);
			this.DisplayValue = this.Format(Value);
		}

		/// <summary>
		/// Sets the value. Called on the main thread.
		/// </summary>
		/// <param name="Value">The new value.</param>
		/// <param name="ShowChange">If the value is marked as changed for a moment, with the value before, when it differs.</param>
		public void ChangeValue(object Value, bool ShowChange)
		{
			string Before = this.DisplayValue;

			this.UpdateValue(Value);

			if (ShowChange && Before != this.DisplayValue)
				_ = this.ShowChangeAsync(Before);
		}

		/// <summary>
		/// Sets the unit or currency shown after the value when it is a number, such as "EUR".
		/// </summary>
		/// <param name="Unit">The unit, or an empty string.</param>
		public void SetUnit(string Unit)
		{
			this.unit = Unit;
			this.DisplayValue = this.Format(this.Value);
		}

		/// <summary>
		/// Formats a value, with the unit after a number.
		/// </summary>
		/// <param name="Value">The value.</param>
		/// <returns>The value as text.</returns>
		private string Format(object? Value)
		{
			string Text = FormatValue(Value);

			return !string.IsNullOrEmpty(this.unit) && Value is decimal or double or float or int or long
				? Text + " " + this.unit
				: Text;
		}

		/// <summary>
		/// Marks the value as changed and shows the value before, until the highlight time has passed or the value changes
		/// again.
		/// </summary>
		/// <param name="Before">The value before, as shown to the user.</param>
		private async Task ShowChangeAsync(string Before)
		{
			int Version = ++this.changeVersion;

			try
			{
				this.PreviousValue = Before;
				this.IsChanged = true;

				await Task.Delay(changeHighlightTime);

				if (Version != this.changeVersion)
					return;

				this.IsChanged = false;
				this.PreviousValue = null;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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
				IDictionary Dictionary => ServiceRef.Localizer[nameof(AppResources.ValuesRows), Dictionary.Count],
				ICollection Collection => ServiceRef.Localizer[nameof(AppResources.ValuesRows), Collection.Count],
				_ => Expression.ToExpressionString(Value)
			};
		}

		#region Commands

		/// <summary>
		/// Opens a list on its own page, or copies another value to the clipboard.
		/// </summary>
		[RelayCommand]
		private async Task OpenAsync()
		{
			if (!this.IsList)
			{
				await this.CopyToClipboard();
				return;
			}

			try
			{
				Variables Rows = [];
				int Index = 0;

				if (this.Value is IDictionary Dictionary)
				{
					foreach (DictionaryEntry Entry in Dictionary)
						Rows[Entry.Key?.ToString() ?? string.Empty] = Entry.Value;
				}
				else if (this.Value is IEnumerable Items)
				{
					foreach (object? Item in Items)
						Rows[(++Index).ToString(CultureInfo.InvariantCulture)] = Item;
				}

				MachineVariablesNavigationArgs Args = new(this.DisplayName, Rows);
				await ServiceRef.NavigationService.GoToAsync(nameof(MachineVariablesPage), Args, BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

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
