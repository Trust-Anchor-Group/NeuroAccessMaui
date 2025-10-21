using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Full-screen popup surface. Derived classes decide how to render their content.
	/// </summary>
	public class BasePopupView : ContentView, ILifeCycleView
	{
		public static readonly BindableProperty PopupContentProperty = BindableProperty.Create(
			nameof(PopupContent),
			typeof(View),
			typeof(BasePopupView),
			null,
			propertyChanged: OnPopupContentChanged);

		private readonly Grid root;
		private readonly ContentView contentHost;

		public BasePopupView()
		{
			this.contentHost = new ContentView
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};

			this.root = new Grid
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};
			this.root.Add(this.contentHost);

			base.Content = this.root;
		}

		/// <summary>
		/// Raw content rendered across the popup surface.
		/// </summary>
		public View? PopupContent
		{
			get => (View?)this.GetValue(PopupContentProperty);
			set => this.SetValue(PopupContentProperty, value);
		}

		public virtual Task OnInitializeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisposeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnAppearingAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisappearingAsync()
		{
			return Task.CompletedTask;
		}

		private static void OnPopupContentChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView)
				popupView.contentHost.Content = newValue as View;
		}

		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			if (propertyName == nameof(Content) && base.Content is View content && !ReferenceEquals(content, this.root))
			{
				base.Content = this.root;
				this.PopupContent = content;
			}
		}

	}
}
