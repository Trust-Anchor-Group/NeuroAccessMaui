using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using System;

namespace NeuroAccessMaui.UI.Pages.Utility
{
	public partial class ScriptConsolePage
	{
		/// <summary>
		/// Initializes a new instance of <see cref="ScriptConsolePage"/>.
		/// </summary>
		public ScriptConsolePage(ScriptConsoleViewModel ViewModel)
		{
			this.InitializeComponent();

			this.BindingContext = ViewModel;
		}
	}
}
