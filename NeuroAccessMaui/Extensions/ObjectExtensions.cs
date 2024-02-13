using System.Reflection;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Generic extension class for objects of type <see cref="object"/>.
	/// </summary>
	public static class ObjectExtensions
	{
		/// <summary>
		/// Returns the current class and method as a <see cref="KeyValuePair{TKey,TValue}"/>. For debugging purposes.
		/// </summary>
		/// <param name="Obj">The object whose class and method to extract.</param>
		/// <param name="MethodInfo">The current method instance.</param>
		/// <param name="Method">An optional method name. If not specified, the method name is extracted from the <c>methodInfo</c> parameter.</param>
		/// <returns>Class and Method</returns>
		public static KeyValuePair<string, object?>[] GetClassAndMethod(this object Obj, MethodBase? MethodInfo, string? Method = null)
		{
			return
				[
					new KeyValuePair<string, object?>("Class", Obj.GetType().Name),
					new KeyValuePair<string, object?>("Method", Method ?? MethodInfo?.Name ?? "UNKNOWN")
				];
		}
	}
}
