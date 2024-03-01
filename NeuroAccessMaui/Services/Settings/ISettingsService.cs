using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Settings
{
	/// <summary>
	/// Handles common runtime settings that need to be persisted during sessions.
	/// </summary>
	[DefaultImplementation(typeof(SettingsService))]
	public interface ISettingsService
	{
		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, string State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, long State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, double State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, bool State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, DateTime State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, TimeSpan State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, Enum State);

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		Task SaveState(string Key, object State);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<string?> RestoreStringState(string Key, string? DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<long> RestoreLongState(string Key, long DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<double> RestoreDoubleState(string Key, double DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<bool> RestoreBoolState(string Key, bool DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<DateTime> RestoreDateTimeState(string Key, DateTime DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<TimeSpan> RestoreTimeSpanState(string Key, TimeSpan DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<Enum?> RestoreEnumState(string Key, Enum? DefaultValueIfNotFound = default);

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <typeparam name="T">The State type.</typeparam>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		Task<T?> RestoreState<T>(string Key, T? DefaultValueIfNotFound = default);

		/// <summary>
		/// Returns any States whose key matches the specified predicate.
		/// </summary>
		/// <typeparam name="T">The State type.</typeparam>
		/// <param name="KeyPrefix">The string value the key should start with, like "Foo". Do not include wildcards.</param>
		/// <returns>a list of matching States.</returns>
		Task<IEnumerable<(string Key, T value)>> RestoreStateWhereKeyStartsWith<T>(string KeyPrefix);

		/// <summary>
		/// Removes a given State.
		/// </summary>
		/// <param name="Key">The State identifier.</param>
		Task RemoveState(string Key);

		/// <summary>
		/// Removes any States whose key matches the specified predicate.
		/// </summary>
		/// <param name="KeyPrefix">The string value the key should start with, like "Foo". Do not include wildcards.</param>
		Task RemoveStateWhereKeyStartsWith(string KeyPrefix);

		/// <summary>
		/// Waits for initialization of the storage service to be completed.
		/// </summary>
		/// <returns>If storage service is OK, or failed to initialize.</returns>
		Task<bool> WaitInitDone();
	}
}
