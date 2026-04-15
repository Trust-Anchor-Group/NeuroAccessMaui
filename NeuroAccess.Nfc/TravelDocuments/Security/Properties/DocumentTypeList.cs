using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties
{
	/// <summary>
	/// Document Type List
	/// </summary>
	public class DocumentTypeList : SecurityObject
	{
		private string[]? types;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.23.136.1.1.6.2";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.types is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElementNested is not Vector DocumentTypeListVector)
				return false;

			if (DocumentTypeListVector.LastElementNested is not Vector DocumentTypes)
				DocumentTypes = DocumentTypeListVector;

			ChunkedList<string> Types = [];

			foreach (object? Item in DocumentTypes)
			{
				if (Item is not string Type)
					return false;

				Types.Add(Type);
			}

			this.types = [.. Types];

			return true;
		}

		/// <summary>
		/// Document types.
		/// </summary>
		public string[] Types => this.types!;
	}
}
