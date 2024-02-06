using System.Collections;
using System.Windows.Input;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// ListView that can load new items when the last items is being displayed
	/// </summary>
	public class LoadingListView : ListView
	{
		/// <summary>
		/// 
		/// </summary>
		public static readonly BindableProperty LoadMoreCommandProperty =
			 BindableProperty.Create(nameof(LoadMoreCommand), typeof(ICommand), typeof(LoadingListView), default(ICommand));

		/// <summary>
		/// Command executed when last item is appearing and new data should be loaded.
		/// </summary>
		public ICommand LoadMoreCommand
		{
			get => (ICommand)this.GetValue(LoadMoreCommandProperty);
			set => this.SetValue(LoadMoreCommandProperty, value);
		}

		/// <summary>
		/// 
		/// </summary>
		public static readonly BindableProperty ItemSelectedCommandProperty =
			 BindableProperty.Create(nameof(ItemSelectedCommand), typeof(ICommand), typeof(LoadingListView), default(ICommand));

		/// <summary>
		/// Command executed when last item is appearing and new data should be loaded.
		/// </summary>
		public ICommand ItemSelectedCommand
		{
			get => (ICommand)this.GetValue(ItemSelectedCommandProperty);
			set => this.SetValue(ItemSelectedCommandProperty, value);
		}

		/// <summary>
		/// ListView that can load new items when the last items is being displayed
		/// </summary>
		public LoadingListView()
		{
			this.ItemAppearing += this.LoadingListView_ItemAppearing;
			this.ItemSelected += this.LoadingListView_ItemSelected;
		}

		private void LoadingListView_ItemAppearing(object? Sender, ItemVisibilityEventArgs e)
		{
			if (this.ItemsSource is IList List && e.Item == List[List.Count - 1])
			{
				if (this.LoadMoreCommand?.CanExecute(null) ?? false)
					this.LoadMoreCommand.Execute(null);
			}
		}

		private void LoadingListView_ItemSelected(object? Sender, SelectedItemChangedEventArgs e)
		{
			if (this.ItemSelectedCommand?.CanExecute(null) ?? false)
				this.ItemSelectedCommand.Execute(this.SelectedItem);
		}

	}
}
