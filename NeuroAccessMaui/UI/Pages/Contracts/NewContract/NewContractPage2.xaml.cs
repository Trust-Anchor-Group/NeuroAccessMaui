using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services;
using Waher.Persistence;
using Waher.Runtime.Profiling.Events;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// A page that allows the user to create a new contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage2 : IContractOptionsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NewContractPage"/> class.
		/// </summary>
		public NewContractPage2()
		{
			this.ContentPageModel = new NewContractViewModel2(this, ServiceRef.UiService.PopLatestArgs<NewContractNavigationArgs>());
			this.InitializeComponent();

			WeakReferenceMessenger.Default.Register<NewContractPageMessage>(this, this.HandleNewContractPageMessage);
			StateContainer.SetCurrentState(this.StateGrid, NewContractStep.Loading.ToString());
		}

		~NewContractPage2()
		{
			WeakReferenceMessenger.Default.Unregister<NewContractPageMessage>(this);
		}

		private async void HandleNewContractPageMessage(object Recipient, NewContractPageMessage Message)
		{
			await this.Dispatcher.DispatchAsync(async () =>
			{
				try
				{
					string OldState = StateContainer.GetCurrentState(this.StateGrid);
					string NewState = Message.NewState.ToString();

					await StateContainer.ChangeStateWithAnimation(this.StateGrid, NewState, CancellationToken.None);


				}
				catch (System.Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
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
