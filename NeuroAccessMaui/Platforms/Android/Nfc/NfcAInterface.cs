﻿using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NFC A Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NfcAInterface(Tag Tag, NfcA Technology)
		: NfcInterface(Tag, Technology), INfcAInterface
	{
		private readonly NfcA nfcA = Technology;

		/// <summary>
		/// Return the ATQA/SENS_RES bytes from tag discovery.
		/// </summary>
		public Task<byte[]> GetAtqa()
		{
			return Task.FromResult(this.nfcA.GetAtqa() ?? throw UnableToReadDataFromDevice());
		}

		/// <summary>
		/// Return the SAK/SEL_RES bytes from tag discovery.
		/// </summary>
		public Task<short> GetSqk()
		{
			return Task.FromResult(this.nfcA.Sak);
		}
	}
}
