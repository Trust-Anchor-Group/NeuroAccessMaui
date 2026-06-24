using System;

namespace NeuroAccess.Nfc.TravelDocuments.Events
{
	/// <summary>
	/// Event arguments for travel document events.
	/// </summary>
	public class TravelDocumentsEventArgs : EventArgs
	{
		/// <summary>
		/// Event arguments for travel document events.
		/// </summary>
		/// <param name="Client">Travel documents client.</param>
		public TravelDocumentsEventArgs(TravelDocumentsClient Client)
		{
			this.Client = Client;
		}

		/// <summary>
		/// Client raising the event.
		/// </summary>
		public TravelDocumentsClient Client { get; }
	}
}
