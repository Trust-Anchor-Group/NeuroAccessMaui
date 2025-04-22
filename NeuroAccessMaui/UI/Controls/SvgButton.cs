using NeuroAccessMaui.UI.Core;

namespace NeuroAccessMaui.UI.Controls
{
    class SvgButton : TemplatedButton , IDisposable
	{
		private readonly SvgView innerSvg;
		private readonly Border innerBorder;

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgSourceProperty = BindableProperty.Create(
			nameof(SvgSource),
			typeof(string),
			typeof(SvgButton),
			propertyChanged: OnSvgSourceChanged);

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgAspectProperty = BindableProperty.Create(
			nameof(SvgAspect),
			typeof(Aspect),
			typeof(SvgButton),
			propertyChanged: OnSvgAspectChanged);

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgTintColorProperty = BindableProperty.Create(
			nameof(SvgTintColor),
			typeof(Color),
			typeof(SvgButton),
			propertyChanged: OnSvgTintColorChanged);

		/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
		public static readonly BindableProperty BorderStyleProperty = BindableProperty.Create(
				nameof(BorderStyle),
				typeof(Style),
				typeof(SvgButton),
				propertyChanged: OnBorderStyleChanged);

		public static void OnSvgSourceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.Source = (string)NewValue;
		}

		public static void OnSvgAspectChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.Aspect = (Aspect)NewValue;
		}

		public static void OnSvgTintColorChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.TintColor = (Color)NewValue;
		}

		public static void OnBorderStyleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerBorder.Style = (Style)NewValue;
		}

		public string SvgSource
		{
			get => (string)this.GetValue(SvgSourceProperty);
			set => this.SetValue(SvgSourceProperty, value);
		}

		public Aspect SvgAspect
		{
			get => (Aspect)this.GetValue(SvgAspectProperty);
			set => this.SetValue(SvgAspectProperty, value);
		}

		public Color SvgTintColor
		{
			get => (Color)this.GetValue(SvgTintColorProperty);
			set => this.SetValue(SvgTintColorProperty, value);
		}

		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
			set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
		}

		public SvgButton()
			: base()
		{
			this.innerSvg = new()
			{
				Aspect = this.SvgAspect,
				WidthRequest = 24,
				HeightRequest = 24,
				TintColor = this.SvgTintColor,
				Source = this.SvgSource
			};
			this.innerBorder = new()
			{
				Content = this.innerSvg,
				WidthRequest = 40,
				HeightRequest = 40,
				Style = this.BorderStyle
			};
			this.Content = this.innerBorder;
		}

		/// <summary>
		/// Releases all resources used by the <see cref="SvgView"/> control.
		/// </summary>
		public void Dispose()
		{
			this.innerSvg.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
