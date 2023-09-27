using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Pages;

public abstract class BaseContentView : ContentView
{
	public static T? Create<T>() where T : BaseContentView
	{
		return ServiceHelper.GetService<T>();
	}

	/// <summary>
	/// Convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
	/// </summary>
	protected BaseViewModel ContentViewModel
	{
		set => this.BindingContext = value;
		get => this.ViewModel<BaseViewModel>();
	}

	/// <summary>
	/// Convenience function for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
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
