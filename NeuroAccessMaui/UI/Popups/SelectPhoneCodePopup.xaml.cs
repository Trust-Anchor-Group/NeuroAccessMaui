using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class SelectPhoneCodePopup : BasePopup, IDisposable
	{
		private readonly TaskCompletionSource<ISO_3166_Country?> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private CancellationTokenSource? cancellationTokenSource;
		private bool isDisposed;

		public Task<ISO_3166_Country?> Result => this.result.Task;

		public static ISO_3166_Country[] Countries => ISO_3166_1.Countries;

		public SelectPhoneCodePopup()
		{
			this.InitializeComponent();
			this.BindingContext = this;
			this.InnerSearchBar.Text = string.Empty;
		}

		private void SearchBar_TextChanged(object? sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.NewTextValue))
			{
				this.InnerListView.ItemsSource = Countries;
				return;
			}

			this.cancellationTokenSource?.Cancel();
			this.cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = this.cancellationTokenSource.Token;

			Task.Run(() =>
			{
				IEnumerable<ISO_3166_Country> filtered = Countries.Where(country =>
					country.Name.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(country.Alpha2, e.NewTextValue, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(country.Alpha3, e.NewTextValue, StringComparison.OrdinalIgnoreCase)
					|| country.DialCode.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase));

				this.Dispatcher.Dispatch(() =>
				{
					if (!token.IsCancellationRequested)
					{
						this.InnerListView.ItemsSource = filtered;
					}
				});
			}, token);
		}

		private async void InnerListView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (this.InnerListView.SelectedItem is ISO_3166_Country selected)
			{
				this.result.TrySetResult(selected);
				await ServiceRef.PopupService.PopAsync();
			}
		}

		public override async Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			await base.OnDisappearingAsync();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.isDisposed)
			{
				if (disposing)
				{
					this.cancellationTokenSource?.Dispose();
					this.cancellationTokenSource = null;
				}

				this.isDisposed = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
