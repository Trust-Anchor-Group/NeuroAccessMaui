using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
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
	public BootstrapErrorPage(string TraceTitle, string TraceText)
	{
		this.TraceTitle = TraceTitle;
		this.TraceText = TraceText;

		this.InitializeComponent();
		this.BindingContext = this;
	}

	public string TraceTitle { get; set; }

	public string TraceText { get; set; }

	[RelayCommand]
	public async Task CopyToClipboard()
	{
		try
		{
			string CopyText = (this.TraceTitle is not null) ? this.TraceTitle : string.Empty;

			if (this.TraceText is not null)
			{
				CopyText += "\n\n";
				CopyText += this.TraceText;
			}

			await Clipboard.SetTextAsync(CopyText);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}
}
