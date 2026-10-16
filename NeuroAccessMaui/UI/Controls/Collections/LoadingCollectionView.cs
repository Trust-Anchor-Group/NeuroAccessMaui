using System.Windows.Input;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// CollectionView that can load new items when the last items is being displayed
	/// </summary>
	public class LoadingCollectionView : CollectionView
	{
		/// <summary>
		///
		/// </summary>
		public static readonly BindableProperty LoadMoreCommandProperty =
			 BindableProperty.Create(nameof(LoadMoreCommand), typeof(ICommand), typeof(LoadingCollectionView), default(ICommand));

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
			 BindableProperty.Create(nameof(ItemSelectedCommand), typeof(ICommand), typeof(LoadingCollectionView), default(ICommand));

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
		public LoadingCollectionView()
		{
			this.RemainingItemsThresholdReached += this.LoadingCollectionView_ThresholdReached;
			this.SelectionChanged += this.LoadingCollectionView_SelectionChanged;
		}

		private void LoadingCollectionView_ThresholdReached(object? Sender, EventArgs e)
		{
			if (this.LoadMoreCommand?.CanExecute(null) ?? false)
				this.LoadMoreCommand.Execute(null);
		}

		private void LoadingCollectionView_SelectionChanged(object? Sender, SelectionChangedEventArgs e)
		{
			if (this.ItemSelectedCommand?.CanExecute(null) ?? false)
				this.ItemSelectedCommand.Execute(this.SelectedItem);
		}

	}
}
