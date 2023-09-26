using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Pages;

public abstract class BaseContentView : ContentView
{
	public static T? Create<T>() where T : BaseContentView
	{
		return ServiceHelper.GetService<T>();
	}

	/// <summary>
	/// Convenience for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
	/// </summary>
	public T ViewModel<T>() where T : BaseViewModel
	{
		if (this.BindingContext is T ViewModel)
		{
			return ViewModel;
		}

		throw new ArgumentException("Wrong view model type: " + nameof(T));
	}

	public BaseContentView()
	{
	}
}
