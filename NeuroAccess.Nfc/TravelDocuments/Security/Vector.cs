using System;
using System.Collections;
using System.Text;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for arrays of security objects.
	/// </summary>
	/// <param name="Elements">Elements in sequence.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public class Vector(Array Elements, byte[] SubSection) : IEnumerable
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
		/// Reference to first element in vector, or null if vector is empty.
		/// </summary>
		public object? FirstElement => this[0];

		/// <summary>
		/// Reference to last element in vector, or null if vector is empty.
		/// </summary>
		public object? LastElement => this[this.Length - 1];

		/// <summary>
		/// Reference to first element in vector, or null if vector is empty.
		/// If the element value is an octet string (byte array), and itself ASN.1 encoded,
		/// the decoded value is returned.
		/// </summary>
		public object? FirstElementNested
		{
			get
			{
				object? First = this.FirstElement;
				if (First is not byte[] Binary)
					return First;

				if (ASN1.TryDecodeDer(Binary, out object? Decoded))
				{
					this.Elements.SetValue(Decoded, 0);
					return Decoded;
				}

				return First;
			}
		}

		/// <summary>
		/// Reference to last element in vector, or null if vector is empty.
		/// If the element value is an octet string (byte array), and itself ASN.1 encoded,
		/// the decoded value is returned.
		/// </summary>
		public object? LastElementNested
		{
			get
			{
				object? Last = this.LastElement;

				if (Last is not byte[] Binary)
					return Last;

				if (ASN1.TryDecodeDer(Binary, out object? Decoded))
				{
					this.Elements.SetValue(Decoded, this.Length - 1);
					return Decoded;
				}

				return Last;
			}
		}

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

			foreach (object? Item in this)
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

		/// <summary>
		/// Gets an enumerator for the elements in the vector.
		/// </summary>
		/// <returns>Enumerator</returns>
		public IEnumerator GetEnumerator()
		{
			return this.Elements.GetEnumerator();
		}
	}
}
