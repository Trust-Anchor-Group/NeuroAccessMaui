using System.ComponentModel;

namespace NeuroAccessMaui.UI.Behaviors;

/// <summary>
/// Used for moving focus to the next UI component when an item has been selected.
/// </summary>
public class SetFocusOnSelectedBehavior : Behavior<Picker>
{
	/// <summary>
	/// The view to move focus to.
	/// </summary>
	[TypeConverter(typeof(ReferenceTypeConverter))]
	public View SetFocusTo { get; set; }

	/// <inheritdoc/>
	protected override void OnAttachedTo(Picker Picker)
	{
		Picker.SelectedIndexChanged += this.Picker_SelectedIndexChanged;
		base.OnAttachedTo(Picker);
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(Picker Picker)
	{
		Picker.SelectedIndexChanged -= this.Picker_SelectedIndexChanged;
		base.OnDetachingFrom(Picker);
	}

	private void Picker_SelectedIndexChanged(object Sender, EventArgs e)
	{
		SetFocusOnClickedBehavior.FocusOn(this.SetFocusTo);
	}
}
