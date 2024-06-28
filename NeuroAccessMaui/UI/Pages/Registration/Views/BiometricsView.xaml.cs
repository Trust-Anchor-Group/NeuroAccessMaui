
namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class BiometricsView
	{
		public static BiometricsView Create()
		{
			return Create<BiometricsView>();
		}

		public BiometricsView(BiometricsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
