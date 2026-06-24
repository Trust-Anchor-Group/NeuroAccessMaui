namespace NeuroAccess.Nfc.TravelDocuments.Events
{
	/// <summary>
	/// Event arguments for travel document state-related events.
	/// </summary>
	public class TravelDocumentsStateEventArgs : TravelDocumentsEventArgs
	{
		/// <summary>
		/// Event arguments for travel document state-related events.
		/// </summary>
		/// <param name="Client">Travel documents client.</param>
		/// <param name="State">Travel documents state.</param>
		/// <param name="AssociatedData">Associated data, if any.</param>
		public TravelDocumentsStateEventArgs(TravelDocumentsClient Client, TravelDocumentsState State,
			object? AssociatedData)
			: base(Client)
		{
			this.State = State;
			this.AssociatedData = AssociatedData;
		}

		/// <summary>
		/// Travel documents state.
		/// </summary>
		public TravelDocumentsState State { get; }

		/// <summary>
		/// Associated data, if any.
		/// </summary>
		public object? AssociatedData { get; }
	}
}
