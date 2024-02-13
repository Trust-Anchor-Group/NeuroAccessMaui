using System.ComponentModel;
using Waher.Events;
using Waher.Things.SensorData;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a sensor-data field.
	/// </summary>
	/// <param name="Field">Sensor-data field.</param>
	public class FieldModel(Field Field) : INotifyPropertyChanged
	{
		private Field field = Field;

		/// <summary>
		/// Field Name
		/// </summary>
		public string Name => this.field.Name;

		/// <summary>
		/// If the field is writable (through the actuator interface)
		/// </summary>
		public bool Writable => this.field.Writable;

		/// <summary>
		/// Quality of service of data
		/// </summary>
		public FieldQoS QoS => this.field.QoS;

		/// <summary>
		/// Field Type
		/// </summary>
		public FieldType Type => this.field.Type;

		/// <summary>
		/// Timestamp of field.
		/// </summary>
		public DateTime Timestamp => this.field.Timestamp;

		/// <summary>
		/// Value of field, as a string.
		/// </summary>
		public string ValueString
		{
			get
			{
				if (this.field is DateTimeField DT)
					return DT.Value.ToShortDateString() + ", " + DT.Value.ToLongTimeString();
				else if (this.field is DateField D)
					return D.Value.ToShortDateString();
				else
					return this.field.ValueString;
			}
		}

		/// <summary>
		/// Horizontal alignment of value.
		/// </summary>
		public TextAlignment HorizontalAlignment
		{
			get
			{
				if (this.field is Int32Field ||
					this.field is Int64Field ||
					this.field is QuantityField)
				{
					return TextAlignment.End;
				}
				else if (this.field is BooleanField ||
					this.field is DateField ||
					this.field is DateTimeField ||
					this.field is DurationField ||
					this.field is TimeField)
				{
					return TextAlignment.Center;
				}
				else
					return TextAlignment.Start;
			}
		}

		/// <summary>
		/// Underlying field object.
		/// </summary>
		public Field Field
		{
			get => this.field;
			set
			{
				bool NameChanged = this.field.Name != value.Name;
				bool WritableChanged = this.field.Writable != value.Writable;
				bool QoSChanged = this.field.QoS != value.QoS;
				bool TypeChanged = this.field.Type != value.Type;
				bool TimestampChanged = this.field.Timestamp != value.Timestamp;
				bool ValueStringChanged = this.field.ValueString != value.ValueString;

				this.field = value;

				if (NameChanged)
					this.RaisePropertyChanged(nameof(this.Name));

				if (WritableChanged)
					this.RaisePropertyChanged(nameof(this.Writable));

				if (QoSChanged)
					this.RaisePropertyChanged(nameof(this.QoS));

				if (TypeChanged)
					this.RaisePropertyChanged(nameof(this.Type));

				if (TimestampChanged)
					this.RaisePropertyChanged(nameof(this.Timestamp));

				if (ValueStringChanged)
					this.RaisePropertyChanged(nameof(this.ValueString));
			}
		}

		private void RaisePropertyChanged(string Name)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
