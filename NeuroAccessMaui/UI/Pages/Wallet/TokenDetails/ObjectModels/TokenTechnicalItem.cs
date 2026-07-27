using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.ObjectModels
{
	/// <summary>
	/// Provides one page-ready technical token value and its explicit copy or open behavior.
	/// </summary>
	public sealed class TokenTechnicalItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenTechnicalItem"/> class.
		/// </summary>
		/// <param name="Label">Localized label describing the value.</param>
		/// <param name="Value">Display value.</param>
		/// <param name="IsSensitive">Whether the value should remain progressively disclosed.</param>
		/// <param name="CopyCommand">Optional command that copies the complete value.</param>
		/// <param name="OpenCommand">Optional command that opens the related item.</param>
		public TokenTechnicalItem(
			string Label,
			string Value,
			bool IsSensitive,
			IAsyncRelayCommand? CopyCommand = null,
			IAsyncRelayCommand? OpenCommand = null)
		{
			this.Label = Label?.Trim() ?? string.Empty;
			this.Value = Value?.Trim() ?? string.Empty;
			this.IsSensitive = IsSensitive;
			this.CopyCommand = CopyCommand;
			this.OpenCommand = OpenCommand;
		}

		/// <summary>
		/// Gets the localized label describing the value.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the display value.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Gets a value indicating whether the item has a display value.
		/// </summary>
		public bool HasValue => !string.IsNullOrWhiteSpace(this.Value);

		/// <summary>
		/// Gets a value indicating whether the value should remain progressively disclosed.
		/// </summary>
		public bool IsSensitive { get; }

		/// <summary>
		/// Gets a value indicating whether the complete value can be copied.
		/// </summary>
		public bool IsCopyable => this.CopyCommand is not null;

		/// <summary>
		/// Gets a value indicating whether the related item can be opened.
		/// </summary>
		public bool IsOpenable => this.OpenCommand is not null;

		/// <summary>
		/// Gets the optional command that copies the complete value.
		/// </summary>
		public IAsyncRelayCommand? CopyCommand { get; }

		/// <summary>
		/// Gets the optional command that opens the related item.
		/// </summary>
		public IAsyncRelayCommand? OpenCommand { get; }
	}
}
