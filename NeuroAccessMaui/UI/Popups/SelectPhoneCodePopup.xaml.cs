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
		private bool isDisposed;

		public Task<ISO_3166_Country?> Result => this.result.Task;

		public SelectPhoneCodePopup()
		{
			this.InitializeComponent();
			this.BindingContext = this;
		}

		private async void PhoneCountrySelectionView_CountrySelected(object? Sender, ISO_3166_Country SelectedCountry)
		{
			this.result.TrySetResult(SelectedCountry);
			await ServiceRef.PopupService.PopAsync();
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
