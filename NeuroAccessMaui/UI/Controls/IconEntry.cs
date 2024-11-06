using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	public class IconEntry : CompositeEntry
	{
		#region Properties
		/// <summary>
		/// Bindable property for the view displayed on the right side of the entry.
		/// </summary>
		public static readonly BindableProperty IconProperty = BindableProperty.Create(
			 nameof(IconPath),
			 typeof(Geometry),
			 typeof(IconEntry)
			 );

		/// <summary>
		/// Gets or sets the view displayed on the right side of the entry.
		/// </summary>
		public Geometry IconPath
		{
			get => (Geometry)this.GetValue(IconProperty);
			set => this.SetValue(IconProperty, value);
		}
		#endregion

		public IconEntry() : base()
		{
			Path iconPath = new Path();
			iconPath.SetBinding(Path.DataProperty, new Binding(nameof(this.IconPath), source: this));
			iconPath.SetBinding(Path.FillProperty, new Binding(nameof(this.TextColor), source: this));

			iconPath.VerticalOptions = LayoutOptions.Center;
			iconPath.HorizontalOptions = LayoutOptions.Center;
			iconPath.WidthRequest = 24;
			iconPath.HeightRequest = 24;
			iconPath.Aspect = Microsoft.Maui.Controls.Stretch.Fill;
			this.LeftView = iconPath;
		}
	}
}
