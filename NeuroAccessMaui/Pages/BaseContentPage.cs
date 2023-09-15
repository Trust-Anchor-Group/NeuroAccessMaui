using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.Pages;

public abstract partial class BaseContentPage<T> : ContentPage
{
	/// <summary>
	/// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
	/// </summary>
	public T ViewModel => (T)this.BindingContext;

	public BaseContentPage()
	{
	}
}
