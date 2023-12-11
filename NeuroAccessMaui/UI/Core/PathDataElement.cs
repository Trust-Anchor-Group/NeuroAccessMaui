using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Core
{
	internal static class PathDataElement
	{
		/// <summary>Bindable property for <see cref="IPathDataElement.PathData"/>.</summary>
		public static readonly BindableProperty PathDataProperty =
			BindableProperty.Create(nameof(IPathDataElement.PathData), typeof(Geometry), typeof(IPathDataElement), default(Geometry),
									propertyChanged: OnPathDataPropertyChanged);

		static void OnPathDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((IPathDataElement)Bindable).OnPathDataPropertyChanged((Geometry)OldValue, (Geometry)NewValue);
		}

		/// <summary>Bindable property for <see cref="IPathDataElement.PathStyle"/>.</summary>
		public static readonly BindableProperty PathStyleProperty =
			BindableProperty.Create(nameof(IPathDataElement.PathStyle), typeof(Style), typeof(IPathDataElement), default(Style),
									propertyChanged: OnPathStylePropertyChanged);

		static void OnPathStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((IPathDataElement)Bindable).OnPathStylePropertyChanged((Style)OldValue, (Style)NewValue);
		}
	}
}
