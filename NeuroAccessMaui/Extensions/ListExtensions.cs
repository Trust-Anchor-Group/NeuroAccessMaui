namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="List{T}"/> class.
	/// </summary>
	public static class ListExtensions
	{
		/// <summary>
		/// Compares to lists for content equality.
		/// </summary>
		/// <typeparam name="T">The list content type.</typeparam>
		/// <param name="list">The first list to compare.</param>
		/// <param name="other">The second list to compare.</param>
		/// <returns>If elements are equal.</returns>
		public static bool HasSameContentAs<T>(this List<T> list, List<T> other)
		{
			if (list.Count != other.Count)
				return false;

			List<T> FirstNotSecond = list.Except(other).ToList();
			List<T> SecondNotFirst = other.Except(list).ToList();
			return FirstNotSecond.Count == 0 && SecondNotFirst.Count == 0;
		}
	}
}
