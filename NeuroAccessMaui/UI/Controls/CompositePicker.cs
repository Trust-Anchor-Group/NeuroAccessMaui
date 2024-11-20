using NeuroAccessMaui.UI.Controls.Extended;
using System.Collections;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Converters;

namespace NeuroAccessMaui.UI.Controls
{
	public class CompositePicker : CompositeInputView
	{
		#region Bindable Properties
		/// <summary>
		/// The font property
		/// </summary>
		public static readonly BindableProperty FontProperty =
			 BindableProperty.Create(nameof(Font), typeof(Microsoft.Maui.Font), typeof(CompositePicker), new Microsoft.Maui.Font());

		/// <summary>
		/// Gets or sets the Font
		/// </summary>
		public Microsoft.Maui.Font Font
		{
			get { return (Microsoft.Maui.Font)this.GetValue(FontProperty); }
			set { this.SetValue(FontProperty, value); }
		}

		/// <summary>
		/// The XAlign property
		/// </summary>
		public static readonly BindableProperty XAlignProperty =
			 BindableProperty.Create(nameof(XAlign), typeof(TextAlignment), typeof(CompositePicker),
			 TextAlignment.Start);

		/// <summary>
		/// Gets or sets the X alignment of the text
		/// </summary>
		public TextAlignment XAlign
		{
			get { return (TextAlignment)this.GetValue(XAlignProperty); }
			set { this.SetValue(XAlignProperty, value); }
		}

		/// <summary>
		/// The HasBorder property
		/// </summary>
		public static readonly BindableProperty HasBorderProperty =
			 BindableProperty.Create(nameof(HasBorder), typeof(bool), typeof(CompositePicker), true);

		/// <summary>
		/// Gets or sets if the border should be shown or not
		/// </summary>
		public bool HasBorder
		{
			get { return (bool)this.GetValue(HasBorderProperty); }
			set { this.SetValue(HasBorderProperty, value); }
		}

		/// <summary>
		/// The Placeholder property
		/// </summary>
		public static readonly BindableProperty PlaceholderProperty =
			 BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(CompositePicker), string.Empty, BindingMode.OneWay);

		/// <summary>
		/// Get or sets the PlaceHolder
		/// </summary>
		public string Placeholder
		{
			get { return (string)this.GetValue(PlaceholderProperty); }
			set { this.SetValue(PlaceholderProperty, value); }
		}

		/// <summary>
		/// The PlaceholderTextColor property
		/// </summary>
		public static readonly BindableProperty PlaceholderTextColorProperty =
			 BindableProperty.Create(nameof(PlaceholderTextColor), typeof(Color), typeof(CompositePicker), Colors.Transparent);

		/// <summary>
		/// Sets color for placeholder text
		/// </summary>
		public Color PlaceholderTextColor
		{
			get { return (Color)this.GetValue(PlaceholderTextColorProperty); }
			set { this.SetValue(PlaceholderTextColorProperty, value); }
		}

		/// <summary>
		/// The TextColor property
		/// </summary>
		public static readonly BindableProperty TextColorProperty =
			BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(CompositePicker), Colors.Black);

		/// <summary>
		/// Gets or sets the TextColor
		/// </summary>
		public Color TextColor
		{
			get => (Color)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the picker
		/// </summary>
		public static readonly BindableProperty PickerStyleProperty =
			BindableProperty.Create(nameof(PickerStyle), typeof(Style), typeof(CompositePicker), null);

		public Style PickerStyle
		{
			get => (Style)this.GetValue(PickerStyleProperty);
			set => this.SetValue(PickerStyleProperty, value);
		}

		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(CompositePicker), null);

		public IList ItemsSource
		{
			get => (IList)this.GetValue(ItemsSourceProperty);
			set => this.SetValue(ItemsSourceProperty, value);
		}

		public static readonly BindableProperty SelectedItemProperty =
			BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(CompositePicker), null, BindingMode.TwoWay);

		public object SelectedItem
		{
			get => (object)this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}

		public BindingBase ItemDisplayBinding
		{
			get => this.picker.ItemDisplayBinding;
			set => this.picker.ItemDisplayBinding = value;
		}

		#endregion

		#region Fields

		private readonly Picker picker;

		#endregion

		public CompositePicker()
		{
			this.picker = new Picker();

			this.picker.SetBinding(Picker.TitleProperty, new Binding(nameof(this.Placeholder), source: this));
			this.picker.SetBinding(Picker.TextColorProperty, new Binding(nameof(this.TextColor), source: this));
			//this.picker.SetBinding(Picker.FontProperty, new Binding(nameof(this.Font), source: this));
			this.picker.SetBinding(Picker.HorizontalTextAlignmentProperty, new Binding(nameof(this.XAlign), source: this));
			this.picker.SetBinding(Picker.BackgroundColorProperty, new Binding(nameof(this.BackgroundColor), source: this));
			this.picker.SetBinding(Picker.StyleProperty, new Binding(nameof(this.PickerStyle), source: this));
			this.picker.SetBinding(Picker.SelectedItemProperty, new Binding(nameof(this.SelectedItem), source: this, mode: BindingMode.TwoWay));
			this.picker.SetBinding(Picker.ItemsSourceProperty, new Binding(nameof(this.ItemsSource), source: this));

			this.CenterView = this.picker;
		}
	}
}
