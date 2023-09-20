namespace NeuroAccessMaui.Extensions;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/> objects.
/// </summary>
public static class EnumerableExtensions
{
	/// <summary>
	/// Creates an <see cref="IEnumerable{TSource}"/> from the specified item.
	/// </summary>
	/// <typeparam name="TSource">The desired type.</typeparam>
	/// <param name="item">The item to convert to an IEnumerable.</param>
	/// <returns>Array of one item</returns>
	public static IEnumerable<TSource> Create<TSource>(TSource item)
	{
		return new[] { item };
	}
}
