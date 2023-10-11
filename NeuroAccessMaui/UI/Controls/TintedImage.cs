using CommunityToolkit.Maui.Behaviors;

namespace NeuroAccessMaui.UI.Controls;

public class TintedImage : Image
{
	public TintedImage()
	{
	}

	public static readonly BindableProperty TintColorProperty = BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(TintedImage), defaultValue: Colors.Transparent,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			TintedImage TintedImage = (TintedImage)Bindable;
			TintedImage.Behaviors.Clear();
			TintedImage.Behaviors.Add(new IconTintColorBehavior() { TintColor = (Color)NewVal });
		});

	public Color TintColor
	{
		get => (Color)this.GetValue(TintColorProperty);
		set => this.SetValue(TintColorProperty, value);
	}
}
