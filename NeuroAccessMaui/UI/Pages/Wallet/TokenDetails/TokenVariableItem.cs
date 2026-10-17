using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A variable of a token's state machine, shown as a row in the State card of token details. When its value changes
	/// while the page is open, the row is tinted for a moment and shows the value it had before.
	/// </summary>
	/// <param name="Name">Name of variable</param>
	/// <param name="Value">Value of variable</param>
	public partial class TokenVariableItem(string Name, object Value) : VariableModel(Name, Value), IListCardItem
	{
		/// <summary>
		/// How long a changed value stays tinted.
		/// </summary>
		private static readonly TimeSpan changeHighlightTime = TimeSpan.FromSeconds(4);

		private int changeVersion;

		/// <summary>
		/// Where the row sits in the State card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// If the value changed a moment ago.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private bool isChanged;

		/// <summary>
		/// The value before it changed, such as "was 12", while the row is tinted.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private string? previousValueText;

		/// <summary>
		/// What screen readers say about the row: the name, the value, and the value before when it changed a moment ago.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				string Result = this.Name + ", " + this.AsScript;

				if (this.IsChanged && !string.IsNullOrEmpty(this.PreviousValueText))
					Result += ", " + this.PreviousValueText;

				return Result;
			}
		}

		/// <summary>
		/// Sets the value. Called on the main thread.
		/// </summary>
		/// <param name="Value">The new value.</param>
		/// <param name="ShowChange">If the row is tinted for a moment, with the value before, when the value differs.</param>
		public void ChangeValue(object Value, bool ShowChange)
		{
			string Before = this.AsScript;

			this.UpdateValue(Value);
			this.OnPropertyChanged(nameof(this.AccessibleDescription));

			if (ShowChange && Before != this.AsScript)
				_ = this.ShowChangeAsync(Before);
		}

		/// <summary>
		/// Tints the row and shows the value before, until the highlight time has passed or the value changes again.
		/// </summary>
		/// <param name="Before">The value before, as script.</param>
		private async Task ShowChangeAsync(string Before)
		{
			int Version = ++this.changeVersion;

			try
			{
				this.PreviousValueText = ServiceRef.Localizer[nameof(AppResources.ValueWas), Before];
				this.IsChanged = true;

				await Task.Delay(changeHighlightTime);

				if (Version != this.changeVersion)
					return;

				this.IsChanged = false;
				this.PreviousValueText = null;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
