using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for arrays of security objects.
	/// </summary>
	/// <param name="Elements">Elements in sequence.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public class Vector(Array Elements, byte[] SubSection)
	{
		/// <summary>
		/// Binary Value.
		/// </summary>
		public Array Elements { get; } = Elements;

		/// <summary>
		/// Subsection
		/// </summary>
		public byte[] SubSection { get; } = SubSection;

		/// <summary>
		/// Length of vector.
		/// </summary>
		public int Length => this.Elements.Length;

		/// <summary>
		/// Gets the element at the specified zero-based index in the collection.
		/// </summary>
		/// <param name="Index">The zero-based index of the element to retrieve. Must be within the valid range of the collection.</param>
		/// <returns>The element at the specified index, or null if the index is outside the bounds of the collection.</returns>
		public object? this[int Index]
		{
			get
			{
				if (Index < 0 || Index >= this.Elements.Length)
					return null;
				else
					return this.Elements.GetValue(Index);
			}
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new();
			bool First = true;

			sb.Append(this.GetType().Name);
			sb.Append('(');

			foreach (object? Item in this.Elements)
			{
				if (First)
					First = false;
				else
					sb.Append(',');

				sb.Append(Item?.ToString() ?? "null");
			}

			sb.Append(')');

			return sb.ToString();
		}
	}
}
