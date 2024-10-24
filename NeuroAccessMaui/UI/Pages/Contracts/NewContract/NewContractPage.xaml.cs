﻿using NeuroAccessMaui.Services;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// A page that allows the user to create a new contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class NewContractPage : IContractOptionsPage
	{

		public ScrollView? ScrollView;
		/// <summary>
		/// Creates a new instance of the <see cref="NewContractPage"/> class.
		/// </summary>
		public NewContractPage()
		{
			this.InitializeComponent();
			NewContractNavigationArgs? args = ServiceRef.UiService.PopLatestArgs<NewContractNavigationArgs>();

			this.ContentPageModel = new NewContractViewModel(this,args );
			this.ScrollView = this.MainScrollView;

			Unloaded += (object? sender, EventArgs e) =>
			{
				(this.ContentPageModel as IDisposable)?.Dispose();
			};
		}

		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		public async Task ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			if (this.ContentPageModel is NewContractViewModel ViewModel)
				await ViewModel.ShowContractOptions(Options);
		}

	}
}
