using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Settings;

/// <summary>
/// Handles common UI settings that need to be persisted during sessions.
/// </summary>
[DefaultImplementation(typeof(SettingsService))]
public interface ISettingsService
{
	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, string state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, long state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, double state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, bool state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, DateTime state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, TimeSpan state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, Enum state);

	/// <summary>
	/// Saves state with the given key.
	/// </summary>
	/// <param name="key">The key to use.</param>
	/// <param name="state">The state to save.</param>
	Task SaveState(string key, object state);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<string> RestoreStringState(string key, string defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<long> RestoreLongState(string key, long defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<double> RestoreDoubleState(string key, double defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<bool> RestoreBoolState(string key, bool defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<DateTime> RestoreDateTimeState(string key, DateTime defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<TimeSpan> RestoreTimeSpanState(string key, TimeSpan defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<Enum> RestoreEnumState(string key, Enum defaultValueIfNotFound = default);

	/// <summary>
	/// Restores state for the specified key.
	/// </summary>
	/// <typeparam name="T">The state type.</typeparam>
	/// <param name="key">The state id.</param>
	/// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
	/// <returns>Value corresponding to the key.</returns>
	Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default);

	/// <summary>
	/// Returns any states whose key matches the specified predicate.
	/// </summary>
	/// <typeparam name="T">The state type.</typeparam>
	/// <param name="keyPrefix">The string value the key should start with, like "Foo". Do not include wildcards.</param>
	/// <returns>a list of matching states.</returns>
	Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix);

	/// <summary>
	/// Removes a given state.
	/// </summary>
	/// <param name="key">The state identifier.</param>
	Task RemoveState(string key);

	/// <summary>
	/// Removes any states whose key matches the specified predicate.
	/// </summary>
	/// <param name="keyPrefix">The string value the key should start with, like "Foo". Do not include wildcards.</param>
	Task RemoveStateWhereKeyStartsWith(string keyPrefix);

	/// <summary>
	/// Waits for initialization of the storage service to be completed.
	/// </summary>
	/// <returns>If storage service is OK, or failed to initialize.</returns>
	Task<bool> WaitInitDone();
}
