namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class LoadingView
{
	public static LoadingView? Create()
	{
		return Create<LoadingView>();
	}

	public LoadingView(LoadingViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
	}

	/// <inheritdoc/>
	protected override async void OnParentSet()
	{
		base.OnParentSet();
		await this.LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
	}
}
