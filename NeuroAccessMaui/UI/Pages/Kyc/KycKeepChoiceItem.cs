using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// An optional verified detail that the user can choose to keep on, or leave out of, the finished identity.
	/// </summary>
	/// <remarks>
	/// The detail is always sent with the preview application. The choice only decides whether it remains on the identity issued afterwards.
	/// </remarks>
	public sealed partial class KycKeepChoiceItem : ObservableObject
	{
		private readonly Action<KycKeepChoiceItem> keptChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycKeepChoiceItem"/> class.
		/// </summary>
		/// <param name="Label">The detail label, such as "Date of birth".</param>
		/// <param name="Value">The detail value as shown in the summary.</param>
		/// <param name="PropertyKeys">The identity property keys the choice applies to.</param>
		/// <param name="IsKept">Whether the detail is currently kept on the finished identity.</param>
		/// <param name="KeptChanged">Called when the user changes the choice.</param>
		public KycKeepChoiceItem(string Label, string Value, IReadOnlyList<string> PropertyKeys, bool IsKept, Action<KycKeepChoiceItem> KeptChanged)
		{
			this.Label = Label;
			this.Value = Value;
			this.PropertyKeys = PropertyKeys;
			this.isKept = IsKept;
			this.keptChanged = KeptChanged;
		}

		/// <summary>
		/// Gets the detail label.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the detail value as shown in the summary.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Gets the identity property keys the choice applies to.
		/// </summary>
		public IReadOnlyList<string> PropertyKeys { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the detail is kept on the finished identity.
		/// </summary>
		[ObservableProperty]
		private bool isKept;

		partial void OnIsKeptChanged(bool value)
		{
			this.keptChanged(this);
		}
	}
}
