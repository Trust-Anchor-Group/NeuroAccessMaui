using System;
using System.Globalization;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Date of Birth
	/// </summary>
	public class DateOfBirth : StringObject
	{
		/// <summary>
		/// Date of Birth
		/// </summary>
		public DateOfBirth()
			: base()
		{
		}

		/// <summary>
		/// Date of Birth
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String Value</param>
		/// <param name="Timestamp">Date of birth</param>
		public DateOfBirth(byte[] Value, string StringValue, DateTime Timestamp)
			: base(Value, StringValue)
		{
			this.Timestamp = Timestamp;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f2b;

		/// <summary>
		/// Date of birth.
		/// </summary>
		public DateTime Timestamp { get; }

		/// <summary>
		/// Creates an instance of the string-valued data object.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="StringValue">String value.</param>
		/// <returns>New instance of string-valued data object.</returns>
		public override StringObject Create(byte[] Value, string StringValue)
		{
			int Year = int.Parse(StringValue.Substring(0, 4), CultureInfo.InvariantCulture);
			int Month = int.Parse(StringValue.Substring(4, 2), CultureInfo.InvariantCulture);
			int Day = int.Parse(StringValue.Substring(6, 2), CultureInfo.InvariantCulture);

			return new DateOfBirth(Value, StringValue, new DateTime(Year, Month, Day));
		}
	}
}
