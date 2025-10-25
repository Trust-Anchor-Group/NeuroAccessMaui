using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

namespace NeuroAccessMaui.UI.Controls
{

	public class AspectRatioLayoutManager : LayoutManager
	{
		public AspectRatioLayoutManager(AspectRatioLayout layout) : base(layout)
		{
		}

		public new AspectRatioLayout Layout => (AspectRatioLayout)base.Layout;

		public override Size Measure(double widthConstraint, double heightConstraint)
		{
			double Aspect = this.Layout.AspectRatio;
			Thickness Padding = this.Layout.Padding;
			double AvailableWidth = Math.Max(0, widthConstraint - Padding.HorizontalThickness);
			double AvailableHeight = Math.Max(0, heightConstraint - Padding.VerticalThickness);

			double Width = AvailableWidth;
			double Height = Width / Aspect;

			if (AvailableHeight > 0 && Height > AvailableHeight)
			{
				Height = AvailableHeight;
				Width = Height * Aspect;
			}

			foreach (IView Child in this.Layout)
			{
				Child.Measure(Width, Height);
			}

			return new Size(Width + Padding.HorizontalThickness, Height + Padding.VerticalThickness);
		}

		public override Size ArrangeChildren(Rect bounds)
		{
			Thickness Padding = this.Layout.Padding;
			double ChildX = Padding.Left;
			double ChildY = Padding.Top;
			double ChildWidth = Math.Max(0, bounds.Width - Padding.HorizontalThickness);
			double ChildHeight = Math.Max(0, bounds.Height - Padding.VerticalThickness);

			foreach (IView Child in this.Layout)
			{
				Child.Arrange(new Rect(ChildX, ChildY, ChildWidth, ChildHeight));
			}

			return bounds.Size;
		}
	}
	public class AspectRatioLayout : Layout
	{
		public static readonly BindableProperty AspectRatioProperty =
			BindableProperty.Create(
				nameof(AspectRatio),
				typeof(double),
				typeof(AspectRatioLayout),
				1.0,
				propertyChanged: OnAspectRatioChanged);

		[TypeConverter(typeof(Converters.AspectRatioTypeConverter))]
		public double AspectRatio
		{
			get { return (double)this.GetValue(AspectRatioProperty); }
			set { this.SetValue(AspectRatioProperty, value); }
		}

		private static void OnAspectRatioChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is AspectRatioLayout AspectRatioView)
			{
				AspectRatioView.InvalidateMeasure();
			}
		}

		protected override ILayoutManager CreateLayoutManager()
		{
			return new AspectRatioLayoutManager(this);
		}
	}
}
