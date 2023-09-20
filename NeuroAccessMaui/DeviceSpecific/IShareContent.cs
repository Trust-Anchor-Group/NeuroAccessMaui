namespace NeuroAccessMaui.DeviceSpecific;

/// <summary>
/// Dependency interface for sharing content with other applications.
/// </summary>
public interface IShareContent
{
	/// <summary>
	/// Shares an image in PNG format.
	/// </summary>
	/// <param name="PngFile">Binary representation (PNG format) of image.</param>
	/// <param name="Message">Message to send with image.</param>
	/// <param name="Title">Title for operation.</param>
	/// <param name="FileName">Filename of image file.</param>
	void ShareImage(byte[] PngFile, string Message, string Title, string FileName);
}
