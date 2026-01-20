using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using Waher.Script.Functions.ComplexNumbers;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	public partial class EmbeddedLayoutViewModel : BaseViewModel
	{
		private readonly EmbeddedLayoutNavigationArgs? navigationArguments;

		[ObservableProperty]
		private ImageSource? renderedLayout;

		[ObservableProperty]
		private bool loaded = false;

		public EmbeddedLayoutViewModel(EmbeddedLayoutNavigationArgs? Args)
		{
			this.navigationArguments = Args;
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (!(navigationArguments is null))
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.RenderedLayout = await navigationArguments.TokenItem.RenderEmbeddedLayout();
					this.Loaded = true;
				});
			}
		}
	}
}
