using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Pages;

public abstract class BaseContentView : ContentView
{
	public static T? Create<T>() where T : BaseContentView
	{
		return ServiceHelper.GetService<T>();
	}

	private Type? viewModelType;

	/// <summary>
	/// Convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
	/// </summary>
	public BaseViewModel ViewModel => (BaseViewModel)this.BindingContext;

	public BaseContentView()
	{
	}

	protected void InitializeObject(BaseViewModel ViewModel)
	{
		this.BindingContext = ViewModel;
		this.viewModelType = ViewModel.GetType();
	}
}
