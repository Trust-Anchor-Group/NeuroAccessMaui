using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Toasts;
using Waher.Content.Xml;

namespace NeuroAccessMaui.UI.Pages.Wallet.XmlViewer
{
	/// <summary>
	/// View model for the page that shows XML, such as a token definition or an XML note.
	/// </summary>
	public partial class XmlViewerViewModel : BaseViewModel
	{
		/// <summary>
		/// View model for the page that shows XML, such as a token definition or an XML note.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public XmlViewerViewModel(XmlViewerNavigationArgs? Args)
		{
			this.Title = Args?.Title ?? string.Empty;
			this.Xml = Indent(Args?.Xml ?? string.Empty);
		}

		/// <summary>
		/// Title shown above the XML.
		/// </summary>
		[ObservableProperty]
		private string title = string.Empty;

		/// <summary>
		/// The XML, indented.
		/// </summary>
		[ObservableProperty]
		private string xml = string.Empty;

		/// <summary>
		/// Copies the XML to the clipboard.
		/// </summary>
		[RelayCommand]
		private async Task CopyXml()
		{
			try
			{
				await Clipboard.Default.SetTextAsync(this.Xml);
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.SuccessfullyCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Shares the XML with other apps.
		/// </summary>
		[RelayCommand]
		private async Task ShareXml()
		{
			try
			{
				await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareTextRequest()
				{
					Title = this.Title,
					Text = this.Xml
				});
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Indents XML for reading. Text that isn't well-formed XML is shown as it is.
		/// </summary>
		/// <param name="Xml">The XML.</param>
		/// <returns>The indented XML.</returns>
		private static string Indent(string Xml)
		{
			if (string.IsNullOrWhiteSpace(Xml))
				return Xml;

			try
			{
				return XML.PrettyXml(Xml);
			}
			catch (Exception)
			{
				return Xml;
			}
		}
	}
}
