using NeuroAccessMaui.UI.Controls.Extended;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	///  Extended DatePicker for nullable values with text placeholder
	/// </summary>
	public class CompositeDatePicker : CompositePicker
	{
	
		/// <summary>
		/// The NullableDate property
		/// </summary>
		public static readonly BindableProperty NullableDateProperty =
			 BindableProperty.Create(nameof(NullableDate), typeof(DateTime?), typeof(CompositeDatePicker), null, BindingMode.TwoWay,
			 propertyChanged: DatePropertyChanged);

		/// <summary>
		/// The MinimumDate property
		/// </summary>
		public static readonly BindableProperty MinimumDateProperty =
			BindableProperty.Create(nameof(MinimumDate), typeof(DateTime), typeof(CompositeDatePicker), DateTime.MinValue);

		/// <summary>
		/// The MaximumDate property
		/// </summary>
		public static readonly BindableProperty MaximumDateProperty =
			BindableProperty.Create(nameof(MaximumDate), typeof(DateTime), typeof(CompositeDatePicker), DateTime.MaxValue);

		/// <summary>
		/// The Date property
		/// </summary>
		public static readonly BindableProperty DateProperty =
			BindableProperty.Create(nameof(Date), typeof(DateTime), typeof(CompositeDatePicker), DateTime.Now, propertyChanged: DatePropertyChanged);

		/// <summary>
		/// Gets or sets the MinimumDate
		/// </summary>
		public DateTime MinimumDate
		{
			get =>(DateTime)this.GetValue(MinimumDateProperty);
			set => this.SetValue(MinimumDateProperty, value);
		}

		/// <summary>
		/// Gets or sets the MaximumDate
		/// </summary>
		public DateTime MaximumDate
		{
			get => (DateTime)this.GetValue(MaximumDateProperty);
			set => this.SetValue(MaximumDateProperty, value);
		}

		/// <summary>
		/// Gets or sets the Date
		/// </summary>
		public DateTime Date
		{
			get => (DateTime)this.GetValue(DateProperty);
			set => this.SetValue(DateProperty, value);
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
		/// Event sent when the date is changed.
		/// </summary>
		public event EventHandler<NullableDateChangedEventArgs>? NullableDateSelected;

		private readonly DatePicker picker;

		/// <summary>
		/// Creates a new instance of the <see cref="CompositeDatePicker"/> class.
		/// </summary>
		public CompositeDatePicker()
		{
			this.picker = new DatePicker();

			this.SetDefaultDate();

			this.picker.SetBinding(DatePicker.DateProperty, new Binding(nameof(this.NullableDate), BindingMode.TwoWay, source: this));
			this.picker.SetBinding(DatePicker.MinimumDateProperty, new Binding(nameof(this.MinimumDate), source: this));
			this.picker.SetBinding(DatePicker.MaximumDateProperty, new Binding(nameof(this.MaximumDate), source: this));
			this.picker.SetBinding(DatePicker.TextColorProperty, new Binding(nameof(this.TextColor), source: this));
			
			this.CenterView = this.picker;
		}

		static void DatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			CompositeDatePicker Picker = (CompositeDatePicker)bindable;
			EventHandler<NullableDateChangedEventArgs>? selected = Picker.NullableDateSelected;

			selected?.Invoke(Picker, new NullableDateChangedEventArgs((DateTime?)oldValue, (DateTime?)newValue));
		}

		private bool isDefaultDateSet = false;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (propertyName == IsFocusedProperty.PropertyName)
				// we don't know if the date picker was closed by the Ok or Cancel button,
				// so we presuppose it was an Ok.
				if (!this.IsFocused)
					this.OnPropertyChanged(nameof(this.picker.DateProperty));

			if (propertyName == nameof(this.picker.DateProperty) && !this.isDefaultDateSet)
				this.NullableDate = this.picker.Date;

			if (propertyName == NullableDateProperty.PropertyName)
				if (this.NullableDate.HasValue)
					this.picker.Date = this.NullableDate.Value;
		}

		private void UpdateDate()
		{
			if (this.NullableDate.HasValue)
				this.picker.Date = this.NullableDate.Value;
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
			this.picker.Date = new DateTime(now.Year, now.Month, now.Day);
		}
	}
}
