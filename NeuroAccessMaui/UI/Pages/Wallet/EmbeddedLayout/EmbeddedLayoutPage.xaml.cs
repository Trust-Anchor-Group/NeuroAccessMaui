using System;
using System.Collections.Generic;
using System.Text;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	/// <summary>
	/// Page used to display a token's embedded layout.
	/// </summary>
	public partial class EmbeddedLayoutPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutPage"/> class.
		/// </summary>
		public EmbeddedLayoutPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new EmbeddedLayoutViewModel(ServiceRef.NavigationService.PopLatestArgs<EmbeddedLayoutNavigationArgs>());
		}
	}
}
