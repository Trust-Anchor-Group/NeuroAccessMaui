using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using Waher.Script.Functions.ComplexNumbers;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	/// <summary>
	/// View model for displaying a rendered embedded layout associated with a token.
	/// </summary>
	public partial class EmbeddedLayoutViewModel : BaseViewModel
	{
		private readonly EmbeddedLayoutNavigationArgs? navigationArguments;

		[ObservableProperty]
		private ImageSource? renderedLayout;

		[ObservableProperty]
		private bool loaded = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments, if any.</param>
		public EmbeddedLayoutViewModel(EmbeddedLayoutNavigationArgs? Args)
		{
			this.navigationArguments = Args;
		}

		/// <summary>
		/// Initializes the view model asynchronously.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (this.navigationArguments is null)
					return;

				if (this.navigationArguments.TokenItem is null)
					return;

				this.RenderedLayout = await this.navigationArguments.TokenItem.RenderEmbeddedLayout();
				this.Loaded = true;
			});
		}
	}
}
