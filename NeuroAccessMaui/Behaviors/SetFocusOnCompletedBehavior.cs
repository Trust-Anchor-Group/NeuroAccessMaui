namespace NeuroAccessMaui.Behaviors;

/// <summary>
/// Used for moving focus to the next UI component when the Enter/Return key has been hit.
/// </summary>
public class SetFocusOnCompletedBehavior : Behavior<Entry>
{
	/// <summary>
	/// The view to move focus to.
	/// </summary>
	[TypeConverter(typeof(ReferenceTypeConverter))]
	public View SetFocusTo { get; set; }

	/// <summary>
	/// Alternative view to move focus to.
	/// </summary>
	[TypeConverter(typeof(ReferenceTypeConverter))]
	public View SetFocusToAlternative { get; set; }

	/// <summary>
	/// Makes <see cref="UseAlternative"/> bindable.
	/// </summary>
	public static readonly BindableProperty UseAlternativeProperty =
		BindableProperty.Create(nameof(UseAlternative), typeof(bool), typeof(SetFocusOnCompletedBehavior), default);

	/// <summary>
	/// If alternative control should be used.
	/// </summary>
	public bool UseAlternative
	{
		get => (bool)this.GetValue(UseAlternativeProperty);
		set => this.SetValue(UseAlternativeProperty, value);
	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(Entry entry)
	{
		entry.Completed += this.Entry_Completed;
		base.OnAttachedTo(entry);
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(Entry entry)
	{
		entry.TextChanged -= this.Entry_Completed;
		base.OnDetachingFrom(entry);
	}

	private void Entry_Completed(object Sender, EventArgs e)
	{
		if (this.UseAlternative && this.SetFocusToAlternative is not null)
			SetFocusOnClickedBehavior.FocusOn(this.SetFocusToAlternative);
		else
			SetFocusOnClickedBehavior.FocusOn(this.SetFocusTo);
	}
}
