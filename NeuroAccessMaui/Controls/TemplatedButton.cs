namespace NeuroAccessMaui.Controls;

/// <summary>
/// TemplatedSwitch represents a generalization of <see cref="Button"/> whose appearance is defined by a <see cref="ControlTemplate"/>.
/// </summary>
public class TemplatedButton : ContentView
{
	/// <summary>
	/// Initializes a new instance of <see cref="TemplatedButton"/> class.
	/// </summary>
	public TemplatedButton()
	{
		TapGestureRecognizer TapRecognizer = new();
		TapRecognizer.Tapped += this.OnTapped;
		this.GestureRecognizers.Add(TapRecognizer);
	}

	private void OnTapped(object? Sender, EventArgs e)
	{
	}
}
