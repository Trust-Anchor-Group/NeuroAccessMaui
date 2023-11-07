using Microsoft.Maui.Controls.Shapes;
using Waher.Content.Html.Elements;

namespace NeuroAccessMaui.UI.Controls;

public partial class ImageButton
{

	/// <summary>
	/// The backing store for the <see cref="Data" /> bindable property.
	/// </summary>
	public static readonly BindableProperty DataProperty = BindableProperty.Create(nameof(Data), typeof(Geometry), typeof(ImageButton), null);

	/// <summary>
	/// Gets or sets the geometry data displayed as the content of the button.
	/// The default value is <see langword="null"/>. This is a bindable property.
	/// </summary>
	public Geometry? Data
	{
		get => (Geometry?)this.GetValue(DataProperty);
		set => this.SetValue(DataProperty, value);
	}

	public ImageButton() : base()
	{
		this.InitializeComponent();
	}
}
