namespace NeuroAccessMaui.UI.Core;

static class PathDataElement
{
	/// <summary>Bindable property for <see cref="IPathDataElement.PathFill"/>.</summary>
	public static readonly BindableProperty PathFillProperty =
		BindableProperty.Create(nameof(IPathDataElement.PathFill), typeof(Brush), typeof(IPathDataElement), default(Brush),
								propertyChanged: OnPathFillPropertyChanged);

	static void OnPathFillPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IPathDataElement)Bindable).OnPathFillPropertyChanged((Brush)OldValue, (Brush)NewValue);
	}

	/// <summary>Bindable property for <see cref="IPathDataElement.PathStroke"/>.</summary>
	public static readonly BindableProperty PathStrokeProperty =
		BindableProperty.Create(nameof(IPathDataElement.PathStroke), typeof(Brush), typeof(IPathDataElement), default(Brush),
								propertyChanged: OnPathStrokePropertyChanged);

	static void OnPathStrokePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IPathDataElement)Bindable).OnPathStrokePropertyChanged((Brush)OldValue, (Brush)NewValue);
	}
}
