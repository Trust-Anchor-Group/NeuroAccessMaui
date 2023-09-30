namespace NeuroAccessMaui.Controls;

/// <summary>
/// TemplatedSwitch represents a generalization of <see cref="Switch"/> whose appearance is defined by a <see cref="ControlTemplate"/>.
/// </summary>
public class TemplatedSwitch : ContentView
{
	/// <summary>
	/// <see cref="BindableProperty"/> implementation of <see cref="IsToggled"/> property.
	/// </summary>
	public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(TemplatedSwitch),
		defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Gets or sets the value indicating if the current <see cref="TemplatedSwitch"/> is toggled.
	/// </summary>
	public bool IsToggled
	{
		get => (bool)this.GetValue(IsToggledProperty);
		set => this.SetValue(IsToggledProperty, value);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TemplatedSwitch"/> class.
	/// </summary>
	public TemplatedSwitch()
	{
		TapGestureRecognizer TapRecognizer = new();
		TapRecognizer.Tapped += this.OnTapped;
		this.GestureRecognizers.Add(TapRecognizer);
	}

	private void OnTapped(object? Sender, System.EventArgs e)
	{
		this.IsToggled ^= true;
	}
}
