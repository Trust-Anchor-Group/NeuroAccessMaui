using Waher.Persistence;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Storage;

/// <summary>
/// Wraps the <see cref="Database"/> for easy access to persistent encrypted storage. Use this for any sensitive data.
/// </summary>
[DefaultImplementation(typeof(StorageService))]
public interface IStorageService
{
	/// <summary>
	/// Folder for database.
	/// </summary>
	string DataFolder { get; }

	#region LifeCycle management

	/// <summary>
	/// Initializes the persistent storage on a background task. This call is asynchronous.
	/// </summary>
	/// <param name="Thread"></param>
	/// <param name="cancellationToken">Will stop the service load if the token is set.</param>
	Task Init(CancellationToken? cancellationToken);

	/// <summary>
	/// Waits for initialization of the storage service to be completed.
	/// </summary>
	/// <returns>If storage service is OK, or failed to initialize.</returns>
	Task<bool> WaitInitDone();

	/// <summary>
	/// Shuts down this persistent storage instance.
	/// </summary>
	Task Shutdown();

	#endregion

	/// <summary>
	/// Inserts an object into the database.
	/// </summary>
	/// <param name="obj">The object to store.</param>
	Task Insert(object obj);

	/// <summary>
	/// Updates an object in the database.
	/// </summary>
	/// <param name="obj">The object to update.</param>
	Task Update(object obj);

	/// <summary>
	/// Returns the first match (if any) of the given type. Deletes the other matching entries.
	/// </summary>
	/// <typeparam name="T">The type of the objects to retrieve.</typeparam>
	Task<T> FindFirstDeleteRest<T>() where T : class;

	/// <summary>
	/// Returns the first match (if any) of the given type. Ignores (leaves) the other matching entries.
	/// </summary>
	/// <typeparam name="T">The type of the objects to retrieve.</typeparam>
	Task<T> FindFirstIgnoreRest<T>() where T : class;

	/// <summary>
	/// Exports the contents of the database to <paramref name="exportOutput"/>.
	/// </summary>
	/// <param name="exportOutput">Receives the contents of the database.</param>
	Task Export(IDatabaseExport exportOutput);

	/// <summary>
	/// Flags the database for repair, so that the next time the app is opened, the database will be repaired.
	/// </summary>
	void FlagForRepair();
}
