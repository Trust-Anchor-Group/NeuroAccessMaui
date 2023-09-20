using System.Collections;
using System.Reflection;

namespace NeuroAccessMaui.Services.UI.Helpers;

public static class VisualTreeHelper
{
	public static T? GetParent<T>(this Element Element) where T : Element
	{
		if (Element is T)
		{
			return Element as T;
		}

		if (Element.Parent is not null)
		{
			return Element.Parent?.GetParent<T>();
		}

		return default;
	}

	public static IEnumerable<T> GetChildren<T>(this Element Element) where T : Element
	{
		IEnumerable<PropertyInfo> Properties = Element.GetType().GetRuntimeProperties();
		PropertyInfo? PropertyInfo = Properties.FirstOrDefault(Property => Property.Name == "Content");

		if (PropertyInfo is not null)
		{
			if (PropertyInfo.GetValue(Element) is Element Content)
			{
				if (Content is T ContentT)
				{
					yield return ContentT;
				}

				foreach (T Child in Content.GetChildren<T>())
				{
					yield return Child;
				}
			}
		}
		else
		{
			PropertyInfo = Properties.FirstOrDefault(Property => Property.Name == "Children");

			if (PropertyInfo is not null)
			{
				IEnumerable? Children = PropertyInfo.GetValue(Element) as IEnumerable;

				if (Children is not null)
				{
					foreach (object? ChildItem in Children)
					{
						if (ChildItem is Element Child)
						{
							if (Child is T ChildT)
							{
								yield return ChildT;
							}

							foreach (T Item in Child.GetChildren<T>())
							{
								yield return Item;
							}
						}
					}
				}
			}
		}
	}
}
