using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	/// <summary>
	/// View model for displaying a rendered embedded layout associated with a token.
	/// </summary>
	public partial class EmbeddedLayoutViewModel : BaseViewModel
	{
		private readonly EmbeddedLayoutNavigationArgs? navigationArguments;

		/// <summary>
		/// The rendered layout.
		/// </summary>
		[ObservableProperty]
		private ImageSource? renderedLayout;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments, if any.</param>
		public EmbeddedLayoutViewModel(EmbeddedLayoutNavigationArgs? Args)
		{
			this.navigationArguments = Args;
		}

		/// <summary>
		/// Renders the layout, and exposes its loading, error and loaded states.
		/// </summary>
		public ObservableTask RenderTask { get; } = new();

		/// <summary>
		/// Initializes the view model asynchronously.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			this.RenderTask.Load(this.RenderAsync);
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			this.RenderTask.Cancel();

			return base.OnDisposeAsync();
		}

		private async Task RenderAsync(TaskContext<int> Context)
		{
			TokenItem? Item = this.navigationArguments?.TokenItem;
			if (Item is null)
				return;

			byte[]? Png = await Item.RenderEmbeddedLayoutPngAsync();
			ImageSource? Source = Png is null ? null : ImageSource.FromStream(() => new MemoryStream(Png));

			await MainThread.InvokeOnMainThreadAsync(() => this.RenderedLayout = Source);
		}
	}
}
