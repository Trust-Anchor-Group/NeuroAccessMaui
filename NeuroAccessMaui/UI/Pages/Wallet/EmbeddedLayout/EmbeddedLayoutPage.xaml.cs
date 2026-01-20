using System;
using System.Collections.Generic;
using System.Text;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	public partial class EmbeddedLayoutPage
	{
		public EmbeddedLayoutPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new EmbeddedLayoutViewModel(ServiceRef.NavigationService.PopLatestArgs<EmbeddedLayoutNavigationArgs>());
		}
	}
}
