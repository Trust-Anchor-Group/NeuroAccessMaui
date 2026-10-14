using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	public class LazyView<T> : ContentView where T : View
	{
		public static readonly BindableProperty DelayMsProperty =
			BindableProperty.Create(
				nameof(DelayMs),
				typeof(int),
				typeof(LazyView<T>),
				100);

		public int DelayMs
		{
			get => (int)this.GetValue(DelayMsProperty);
			set => this.SetValue(DelayMsProperty, value);
		}

		private bool isLoaded = false;

		public LazyView()
		{
			// Start loading after appearing
			this.Loaded += async (s, e) => await this.TryLoadAsync();
		}

		private async Task TryLoadAsync()
		{
			if (this.isLoaded) return;
			this.isLoaded = true;

			if (this.DelayMs > 0)
				await Task.Delay(this.DelayMs);

			T PageOrView = ServiceRef.Provider.GetRequiredService<T>();

			if (PageOrView.BindingContext is null)
				PageOrView.BindingContext = this.BindingContext;

			// (Optional) If you want to support Page or View, you can check here
			if (PageOrView is View v)
				this.Content = v;
			else
				throw new InvalidOperationException($"{typeof(T).Name} is not a View.");
		}
	}
}
