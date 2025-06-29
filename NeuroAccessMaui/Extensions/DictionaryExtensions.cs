using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Rendering;
using Waher.Content.Markdown;
using Waher.Events;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class DictionaryExtensions
	{
		public static TValue? GetValueOrDefault<TKey, TValue>(
			this IReadOnlyDictionary<TKey, TValue> Dict,
			TKey Key,
			TValue? Fallback = default(TValue?))
		{
			return Dict.TryGetValue(Key, out TValue? Value)
				? Value
				: Fallback;
		}
	}
}
