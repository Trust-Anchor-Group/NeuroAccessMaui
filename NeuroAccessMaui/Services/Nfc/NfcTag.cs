namespace NeuroAccessMaui.Services.Nfc;

/// <summary>
/// Class defining interaction with an NFC Tag.
/// </summary>
public class NfcTag : INfcTag
{
	/// <summary>
	/// Class defining interaction with an NFC Tag.
	/// </summary>
	/// <param name="ID">ID of Tag</param>
	/// <param name="Interfaces">Available communication interfaces.</param>
	public NfcTag(byte[] ID, INfcInterface[] Interfaces)
	{
		this.ID = ID;
		this.Interfaces = Interfaces;
	}

	/// <summary>
	/// ID of Tag
	/// </summary>
	public byte[] ID { get; private set; }

	/// <summary>
	/// Available communication interfaces.
	/// </summary>
	public INfcInterface[] Interfaces { get; private set; }

	/// <summary>
	/// <see cref="IDisposable.Dispose"/>
	/// </summary>
	public void Dispose()
	{
		foreach (INfcInterface Interface in this.Interfaces)
		{
			Interface.Dispose();
		}
	}
}
