namespace NeuroAccessMaui.UI.Controls.Extended
{
	/// <summary>
	///  Extended DatePicker for nullable values with text placeholder
	/// </summary>
	public class ExtendedDatePicker : DatePicker
	{
		/// <summary>
		/// The font property
		/// </summary>
		public static readonly BindableProperty FontProperty =
			 BindableProperty.Create(nameof(Font), typeof(Microsoft.Maui.Font), typeof(ExtendedDatePicker), new Microsoft.Maui.Font());

		/// <summary>
		/// The NullableDate property
		/// </summary>
		public static readonly BindableProperty NullableDateProperty =
			 BindableProperty.Create(nameof(NullableDate), typeof(DateTime?), typeof(ExtendedDatePicker), null, BindingMode.TwoWay,
			 propertyChanged: DatePropertyChanged);

		/// <summary>
		/// The XAlign property
		/// </summary>
		public static readonly BindableProperty XAlignProperty =
			 BindableProperty.Create(nameof(XAlign), typeof(TextAlignment), typeof(ExtendedDatePicker),
			 TextAlignment.Start);

		/// <summary>
		/// The HasBorder property
		/// </summary>
		public static readonly BindableProperty HasBorderProperty =
			 BindableProperty.Create(nameof(HasBorder), typeof(bool), typeof(ExtendedDatePicker), true);

		/// <summary>
		/// The Placeholder property
		/// </summary>
		public static readonly BindableProperty PlaceholderProperty =
			 BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ExtendedDatePicker), string.Empty, BindingMode.OneWay);

		/// <summary>
		/// The PlaceholderTextColor property
		/// </summary>
		public static readonly BindableProperty PlaceholderTextColorProperty =
			 BindableProperty.Create(nameof(PlaceholderTextColor), typeof(Color), typeof(ExtendedDatePicker), Colors.Transparent);


		/// <summary>
		/// Gets or sets the Font
		/// </summary>
		public Microsoft.Maui.Font Font
		{
			get { return (Microsoft.Maui.Font)this.GetValue(FontProperty); }
			set { this.SetValue(FontProperty, value); }
		}

		/// <summary>
		/// Get or sets the NullableDate
		/// </summary>
		public DateTime? NullableDate
		{
			get { return (DateTime?)this.GetValue(NullableDateProperty); }
			set
			{
				if (value != this.NullableDate)
				{
					this.SetValue(NullableDateProperty, value);
					this.UpdateDate();
				}
			}
		}

		/// <summary>
		/// Gets or sets the X alignment of the text
		/// </summary>
		public TextAlignment XAlign
		{
			get { return (TextAlignment)this.GetValue(XAlignProperty); }
			set { this.SetValue(XAlignProperty, value); }
		}


		/// <summary>
		/// Gets or sets if the border should be shown or not
		/// </summary>
		public bool HasBorder
		{
			get { return (bool)this.GetValue(HasBorderProperty); }
			set { this.SetValue(HasBorderProperty, value); }
		}

		/// <summary>
		/// Get or sets the PlaceHolder
		/// </summary>
		public string Placeholder
		{
			get { return (string)this.GetValue(PlaceholderProperty); }
			set { this.SetValue(PlaceholderProperty, value); }
		}

		/// <summary>
		/// Sets color for placeholder text
		/// </summary>
		public Color PlaceholderTextColor
		{
			get { return (Color)this.GetValue(PlaceholderTextColorProperty); }
			set { this.SetValue(PlaceholderTextColorProperty, value); }
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ExtendedDatePicker"/> class.
		/// </summary>
		public ExtendedDatePicker() : base()
		{
			this.SetDefaultDate();
		}

		/// <summary>
		/// Event sent when the date is changed.
		/// </summary>
		public event EventHandler<NullableDateChangedEventArgs>? NullableDateSelected;

		static void DatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			ExtendedDatePicker Picker = (ExtendedDatePicker)bindable;
			EventHandler<NullableDateChangedEventArgs>? selected = Picker.NullableDateSelected;

			selected?.Invoke(Picker, new NullableDateChangedEventArgs((DateTime?)oldValue, (DateTime?)newValue));
		}

		private bool isDefaultDateSet = false;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (propertyName == IsFocusedProperty.PropertyName)
			{
				// we don't know if the date picker was closed by the Ok or Cancel button,
				// so we presuppose it was an Ok.
				if (!this.IsFocused)
				{
					this.OnPropertyChanged(DateProperty.PropertyName);
				}
			}

			if (propertyName == DateProperty.PropertyName && !this.isDefaultDateSet)
			{
				this.NullableDate = this.Date;
			}

			if (propertyName == NullableDateProperty.PropertyName)
			{
				if (this.NullableDate.HasValue)
				{
					this.Date = this.NullableDate.Value;
				}
			}
		}

		private void UpdateDate()
		{
			if (this.NullableDate.HasValue)
			{
				this.Date = this.NullableDate.Value;
			}
			else
			{
				this.isDefaultDateSet = true;
				this.SetDefaultDate();
				this.isDefaultDateSet = false;
			}
		}

		private void SetDefaultDate()
		{
			DateTime now = DateTime.Now;
			this.Date = new DateTime(now.Year, now.Month, now.Day);
		}
	}
}
