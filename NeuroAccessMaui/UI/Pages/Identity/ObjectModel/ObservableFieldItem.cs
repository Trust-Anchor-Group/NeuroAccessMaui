using CommunityToolkit.Mvvm.ComponentModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Identity.ObjectModel
{
	/// <summary>
	/// ViewModel for a single property field of a <see cref="LegalIdentity"/>.
	/// </summary>
	/// <remarks>
	/// Also supports custom fields, which are not part of the <see cref="LegalIdentity"/> model. Trough the <see cref="OverrideValue"/> property.
	/// This is for 'fields' created trough composing different fields like BDay, BMonth, BYear -> BDate. Or other things which should be presented as fields.
	/// </remarks>
	public partial class ObservableFieldItem : ObservableObject
	{
		private readonly LegalIdentity model;
		private readonly string key;

		/// <summary>
		/// The value that the user has overridden. This is null if the user has not overridden the value.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Value))]
		private string? overrideValue;

		/// <summary>
		/// If the field is checked during review
		/// </summary>
		[ObservableProperty]
		private bool isChecked;

		/// <summary>
		/// The label for the field.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// If the key is reviewable.
		/// </summary>
		public bool IsReviewable { get; }

		/// <summary>
		/// Either the user‑override (if set), or else the model value.
		/// </summary>
		public string? Value
			=> this.OverrideValue ?? this.model[this.key];

		/// <summary>
		/// ctor.
		/// </summary>
		public ObservableFieldItem(
			string key,
			string label,
			LegalIdentity model,
			bool isReviewable,
			string? initialOverride = null)
		{
			this.key = key;
			this.Label = label;
			this.model = model;
			this.IsReviewable = isReviewable;

			// initialize generated property
			this.OverrideValue = initialOverride;
		}

	}
}
