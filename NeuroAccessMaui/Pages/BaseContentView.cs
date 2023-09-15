namespace NeuroAccessMaui.Pages;

public abstract class BaseContentView<T> : ContentView where T : BaseViewModel
{
	/// <summary>
	/// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
	/// </summary>
	public T ViewModel => (T)this.BindingContext;

	public BaseContentView()
	{
	}
}
