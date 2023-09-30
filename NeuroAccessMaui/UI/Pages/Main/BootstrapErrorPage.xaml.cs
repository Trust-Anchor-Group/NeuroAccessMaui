using System.Windows.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Pages.Main;

/// <summary>
/// A page which is displayed when an unexpected exception is encountered during the application startup.
/// </summary>
public partial class BootstrapErrorPage
{
	/// <summary>
	/// Creates a new instance of the <see cref="BootstrapErrorPage"/> class.
	/// </summary>
	public BootstrapErrorPage(string Title, string StackTrace)
	{
		this.Title = Title;
		this.StackTrace = StackTrace;

		this.InitializeComponent();
	}

	private string Title { get; set; }

	private string StackTrace { get; set; }

	private ICommand CopyToClipboardCommand { get; }

	private async void CopyToClipboard()
	{
		try
		{
			if (!string.IsNullOrEmpty(this.StackTrace))
			{
				await Clipboard.SetTextAsync(this.StackTrace);
			}
		}
		catch (Exception Exception)
		{
			ServiceRef.LogService.LogException(Exception);
		}
	}
}
