using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.UI.Controls.Extended;
using NeuroAccessMaui.UI.Converters;
using Waher.Events;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Extended DatePicker for nullable values with text placeholder.
	/// </summary>
	public class CompositeDatePicker : CompositeInputView
	{
		/// <summary>
		/// The Font property.
		/// </summary>
		public static readonly BindableProperty FontProperty = BindableProperty.Create(
			propertyName: nameof(Font),
			returnType: typeof(Microsoft.Maui.Font),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: new Microsoft.Maui.Font());

		/// <summary>
		/// Gets or sets the font.
		/// </summary>
		public Microsoft.Maui.Font Font
		{
			get => (Microsoft.Maui.Font)this.GetValue(FontProperty);
			set => this.SetValue(FontProperty, value);
		}

		/// <summary>
		/// The XAlign property.
		/// </summary>
		public static readonly BindableProperty XAlignProperty = BindableProperty.Create(
			propertyName: nameof(XAlign),
			returnType: typeof(TextAlignment),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: TextAlignment.Start);

		/// <summary>
		/// Gets or sets the horizontal alignment of the text.
		/// </summary>
		public TextAlignment XAlign
		{
			get => (TextAlignment)this.GetValue(XAlignProperty);
			set => this.SetValue(XAlignProperty, value);
		}

		/// <summary>
		/// The HasBorder property.
		/// </summary>
		public static readonly BindableProperty HasBorderProperty = BindableProperty.Create(
			propertyName: nameof(HasBorder),
			returnType: typeof(bool),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: true);

		/// <summary>
		/// Gets or sets whether the border should be shown.
		/// </summary>
		public bool HasBorder
		{
			get => (bool)this.GetValue(HasBorderProperty);
			set => this.SetValue(HasBorderProperty, value);
		}

		/// <summary>
		/// The Placeholder property.
		/// </summary>
		public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
			propertyName: nameof(Placeholder),
			returnType: typeof(string),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: string.Empty,
			defaultBindingMode: BindingMode.OneWay);

		/// <summary>
		/// Gets or sets the placeholder text to display when no date is selected.
		/// </summary>
		public string Placeholder
		{
			get => (string)this.GetValue(PlaceholderProperty);
			set => this.SetValue(PlaceholderProperty, value);
		}

		/// <summary>
		/// The PlaceholderTextColor property.
		/// </summary>
		public static readonly BindableProperty PlaceholderTextColorProperty = BindableProperty.Create(
			propertyName: nameof(PlaceholderTextColor),
			returnType: typeof(Color),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: Colors.Transparent);

		/// <summary>
		/// Gets or sets the color of the placeholder text.
		/// </summary>
		public Color PlaceholderTextColor
		{
			get => (Color)this.GetValue(PlaceholderTextColorProperty);
			set => this.SetValue(PlaceholderTextColorProperty, value);
		}

		/// <summary>
		/// The TextColor property.
		/// </summary>
		public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
			propertyName: nameof(TextColor),
			returnType: typeof(Color),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: Colors.Black);

		/// <summary>
		/// Gets or sets the color of the selected date text.
		/// </summary>
		public Color TextColor
		{
			get => (Color)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// The PickerStyle property.
		/// </summary>
		public static readonly BindableProperty PickerStyleProperty = BindableProperty.Create(
			propertyName: nameof(PickerStyle),
			returnType: typeof(Style),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: null);

		/// <summary>
		/// Gets or sets the style applied to the internal DatePicker.
		/// </summary>
		public Style PickerStyle
		{
			get => (Style)this.GetValue(PickerStyleProperty);
			set => this.SetValue(PickerStyleProperty, value);
		}

		/// <summary>
		/// The NullableDate property.
		/// </summary>
		public static readonly BindableProperty NullableDateProperty = BindableProperty.Create(
			propertyName: nameof(NullableDate),
			returnType: typeof(DateTime?),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: null,
			defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the selected date, or null if none.
		/// </summary>
		public DateTime? NullableDate
		{
			get => (DateTime?)this.GetValue(NullableDateProperty);
			set => this.SetValue(NullableDateProperty, value);
		}

		/// <summary>
		/// The MinimumDate property.
		/// </summary>
		public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(
			propertyName: nameof(MinimumDate),
			returnType: typeof(DateTime),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: DateTime.MinValue);

		/// <summary>
		/// Gets or sets the minimum allowable date.
		/// </summary>
		public DateTime MinimumDate
		{
			get => (DateTime)this.GetValue(MinimumDateProperty);
			set => this.SetValue(MinimumDateProperty, value);
		}

		/// <summary>
		/// The MaximumDate property.
		/// </summary>
		public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
			propertyName: nameof(MaximumDate),
			returnType: typeof(DateTime),
			declaringType: typeof(CompositeDatePicker),
			defaultValue: DateTime.MaxValue);

		/// <summary>
		/// Gets or sets the maximum allowable date.
		/// </summary>
		public DateTime MaximumDate
		{
			get => (DateTime)this.GetValue(MaximumDateProperty);
			set => this.SetValue(MaximumDateProperty, value);
		}

		/// <summary>
		/// Occurs when the nullable date is changed by user action.
		/// </summary>
		public event EventHandler<NullableDateChangedEventArgs>? NullableDateSelected;

		private readonly WrappedDatePicker picker;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeDatePicker"/> class.
		/// </summary>
		public CompositeDatePicker()
		{
			this.picker = new WrappedDatePicker();
			this.SetDefaultDate();

			// Bind internal properties to this control
			this.picker.SetBinding(DatePicker.DateProperty,
				new Binding(nameof(this.NullableDate), source: this, mode: BindingMode.TwoWay, converter: new NullableDateTimeConverter()));
			this.picker.SetBinding(DatePicker.MinimumDateProperty,
				new Binding(nameof(this.MinimumDate), source: this));
			this.picker.SetBinding(DatePicker.MaximumDateProperty,
				new Binding(nameof(this.MaximumDate), source: this));
			this.picker.SetBinding(DatePicker.TextColorProperty,
				new Binding(nameof(this.TextColor), source: this));
			this.picker.SetBinding(DatePicker.StyleProperty,
				new Binding(nameof(this.PickerStyle), source: this));

			// Apply initial format (placeholder or date)
			this.UpdateFormat();

			// Handle user changes and property updates
			this.picker.DateSelected += this.OnPickerDateSelected;
			if(this.picker is VisualElement E)
				E.Focused += this.OnPickerUnfocused;
			this.PropertyChanged += this.OnControlPropertyChanged;

			this.CenterView = this.picker;
		}

		/// <summary>
		/// Responds to changes in Placeholder or NullableDate to update the displayed format.
		/// </summary>
		private void OnControlPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(this.Placeholder) || E.PropertyName == nameof(this.NullableDate))
				this.UpdateFormat();
		}

		/// <summary>
		/// Handles the internal DatePicker's DateSelected event.
		/// Syncs <see cref="NullableDate"/> and raises <see cref="NullableDateSelected"/>.
		/// </summary>
		private void OnPickerDateSelected(object? Sender, DateChangedEventArgs E)
		{
			var OldDate = this.NullableDate;
			this.NullableDate = E.NewDate;
			NullableDateSelected?.Invoke(this, new NullableDateChangedEventArgs(OldDate, this.NullableDate));
		}

		private void OnPickerUnfocused(object? sender, FocusEventArgs e)
		{
			if (!this.NullableDate.HasValue)
			{
				var Picked = this.picker.Date;
				this.NullableDate = Picked;
				NullableDateSelected?.Invoke(this,
					new NullableDateChangedEventArgs(null, Picked));
			}
		}

		/// <summary>
		/// Updates the <see cref="Picker.Format"/> property based on whether a date is selected.
		/// </summary>
		private void UpdateFormat()
		{
			if (this.NullableDate.HasValue)
			{
				// Use the current culture's short date pattern once a date is chosen.
				this.picker.Format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
			}
			else
			{
				// Escape any single-quotes in the placeholder,
				// then wrap the whole thing in single-quotes.
				string escaped = this.Placeholder.Replace("'", "''");
				this.picker.Format = $"'{escaped}'";
			}
		}

		/// <summary>
		/// Initializes the internal picker to today's date without a time component.
		/// </summary>
		private void SetDefaultDate()
		{
			this.picker.Date = DateTime.Today.Date;
		}
	}
}
