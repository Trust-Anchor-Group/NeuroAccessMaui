using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	public class ImageButton : TemplatedButton, IBorderDataElement, IPathDataElement
	{
		private readonly Border innerBorder;
		private readonly PathShape innerPath;

		/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
		public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

		/// <summary>Bindable property for <see cref="PathData"/>.</summary>
		public static readonly BindableProperty PathDataProperty = PathDataElement.PathDataProperty;

		/// <summary>Bindable property for <see cref="PathStyle"/>.</summary>
		public static readonly BindableProperty PathStyleProperty = PathDataElement.PathStyleProperty;

		public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
		{
			this.innerBorder.Style = NewValue;
		}

		public void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue)
		{
			this.innerPath.Data = NewValue;
		}

		public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
		{
			this.innerPath.Style = NewValue;
		}

		public Geometry PathData
		{
			get => (Geometry)this.GetValue(PathDataElement.PathDataProperty);
			set => this.SetValue(PathDataElement.PathDataProperty, value);
		}

		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
			set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
		}

		public Style PathStyle
		{
			get => (Style)this.GetValue(PathDataElement.PathStyleProperty);
			set => this.SetValue(PathDataElement.PathStyleProperty, value);
		}

		public ImageButton() : base()
		{
			this.innerPath = new()
			{
				HeightRequest = 24,
				WidthRequest = 24,
				Aspect = Stretch.Uniform
			};

			this.innerBorder = new()
			{
				StrokeThickness = 1,
				Content = this.innerPath
			};

			this.Content = this.innerBorder;
		}
	}
}
