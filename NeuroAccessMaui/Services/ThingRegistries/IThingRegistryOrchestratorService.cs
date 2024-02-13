using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.ThingRegistries
{
    /// <summary>
    /// Orchestrates operations on things via Thing Registry services.
    /// </summary>
    [DefaultImplementation(typeof(ThingRegistryOrchestratorService))]
    public interface IThingRegistryOrchestratorService : ILoadableService
    {
        /// <summary>
        /// Opens the Claim Device View, allowing the user to revise device information found in the iotdisco URI provided,
        /// and claim it.
        /// </summary>
        /// <param name="Uri">iotdisco claim URI</param>
        Task OpenClaimDevice(string Uri);

        /// <summary>
        /// Opens the Search Devices View, allowing the user to revise search parameters found in the iotdisco URI provided,
        /// and start the search for devices.
        /// </summary>
        /// <param name="Uri">iotdisco search URI</param>
        Task OpenSearchDevices(string Uri);

        /// <summary>
        /// Opens the Device View, allowing the user to interact with the device defined in the iotdisco URI provided.
        /// </summary>
        /// <param name="Uri">iotdisco direct reference URI</param>
        Task OpenDeviceReference(string Uri);
    }
}
