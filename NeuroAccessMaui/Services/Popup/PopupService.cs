﻿using Mopups.Services;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Popup
{
	[Singleton]
	public class PopupService : IPopupService
	{
		protected readonly Stack<BasePopupViewModel?> viewModelStack = new();

		/// <inheritdoc/>
		public Task PushPopupAsync<TPage, TViewModel>() where TPage : BasePopup, new() where TViewModel : BasePopupViewModel, new()
		{
			TPage page = new();
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushPopupAsync<TPage, TViewModel>(TPage page, TViewModel viewModel) where TPage : BasePopup where TViewModel : BasePopupViewModel
		{
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushPopupAsync<TPage, TViewModel>(TPage page) where TPage : BasePopup where TViewModel : BasePopupViewModel, new()
		{
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushPopupAsync<TPage, TViewModel>(TViewModel viewModel) where TPage : BasePopup, new() where TViewModel : BasePopupViewModel
		{
			TPage page = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public Task PushPopupAsync<TPage>() where TPage : BasePopup, new()
		{
			TPage page = new();
			this.viewModelStack.Push(null);
			return MopupService.Instance.PushAsync(page);
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushPopupAsync<TPage, TViewModel, TReturn>() where TPage : BasePopup, new() where TViewModel : ReturningPopupViewModel<TReturn>, new()
		{
			TPage page = new();
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushPopupAsync<TPage, TViewModel, TReturn>(TPage page, TViewModel viewModel) where TPage : BasePopup where TViewModel : ReturningPopupViewModel<TReturn>
		{
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushPopupAsync<TPage, TViewModel, TReturn>(TPage page) where TPage : BasePopup
			where TViewModel : ReturningPopupViewModel<TReturn>, new()
		{
			TViewModel viewModel = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task<TReturn?> PushPopupAsync<TPage, TViewModel, TReturn>(TViewModel viewModel) where TPage : BasePopup, new() where TViewModel : ReturningPopupViewModel<TReturn>
		{
			TPage page = new();
			page.ViewModel = viewModel;
			this.viewModelStack.Push(viewModel);
			await MopupService.Instance.PushAsync(page);
			return await viewModel.Result;
		}

		/// <inheritdoc/>
		public async Task PopPopupAsync()
		{
			if (this.viewModelStack.Count == 0)
				return;
			BasePopupViewModel? vm = this.viewModelStack.Pop();
			vm?.OnPop();
			await MopupService.Instance.PopAsync();
		}
	}
}
