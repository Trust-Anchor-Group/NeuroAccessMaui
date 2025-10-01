using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Xml
{
	/// <summary>
	/// Contract for validating XML instances against pre-registered XML Schemas (XSD).
	/// Schemas are lazily loaded the first time they are needed.
	/// </summary>
	[DefaultImplementation(typeof(XmlSchemaValidationService))]
	public interface IXmlSchemaValidationService
	{
		/// <summary>
		/// Registers a schema under a logical Key. The schema file must exist in the application package (Raw resources).
		/// Example RelativePath: "Schemas/NeuroAccessKycProcess.xsd".
		/// </summary>
		/// <param name="Key">Logical unique Key.</param>
		/// <param name="RelativePath">Relative path inside the app package.</param>
		void RegisterSchema(string Key, string RelativePath);

		/// <summary>
		/// Validates an XML string against the schema identified by Key.
		/// Returns true if valid or schema missing (non-fatal); false if schema explicitly invalidates the XML.
		/// </summary>
		/// <param name="Key">Schema Key.</param>
		/// <param name="Xml">XML content.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<bool> ValidateAsync(string Key, string Xml, CancellationToken CancellationToken = default);

		/// <summary>
		/// Checks if a schema Key has been registered.
		/// </summary>
		bool IsRegistered(string Key);
	}
}
