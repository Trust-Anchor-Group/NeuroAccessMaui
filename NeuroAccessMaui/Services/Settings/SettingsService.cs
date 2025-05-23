﻿using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Settings
{
	/// <summary>
	/// Handles common runtime settings that need to be persisted during sessions.
	/// </summary>
	[Singleton]
	internal sealed class SettingsService : ISettingsService
	{
		/// <summary>
		/// Handles common runtime settings that need to be persisted during sessions.
		/// </summary>
		public SettingsService()
		{
		}

		private static void AssertNonEmptyPRefix(string KeyPrefix)
		{
			if (string.IsNullOrWhiteSpace(KeyPrefix))
				throw new ArgumentException("Empty key prefix not permitted.", nameof(KeyPrefix));
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, string State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, long State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, double State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, bool State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, DateTime State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, TimeSpan State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, Enum State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Saves State with the given key.
		/// </summary>
		/// <param name="Key">The key to use.</param>
		/// <param name="State">The State to save.</param>
		public async Task SaveState(string Key, object State)
		{
			try
			{
				await RuntimeSettings.SetAsync(Key, State);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<string?> RestoreStringState(string Key, string? DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<long> RestoreLongState(string Key, long DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<double> RestoreDoubleState(string Key, double DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<bool> RestoreBoolState(string Key, bool DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<DateTime> RestoreDateTimeState(string Key, DateTime DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<TimeSpan> RestoreTimeSpanState(string Key, TimeSpan DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<Enum?> RestoreEnumState(string Key, Enum? DefaultValueIfNotFound = default)
		{
			try
			{
				return await RuntimeSettings.GetAsync(Key, DefaultValueIfNotFound);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Restores State for the specified key.
		/// </summary>
		/// <typeparam name="T">The State type.</typeparam>
		/// <param name="Key">The State id.</param>
		/// <param name="DefaultValueIfNotFound">The default value to use if the State isn't found.</param>
		/// <returns>Value corresponding to the key.</returns>
		public async Task<T?> RestoreState<T>(string Key, T? DefaultValueIfNotFound = default)
		{
			if (string.IsNullOrWhiteSpace(Key))
				return DefaultValueIfNotFound;

			try
			{
				object ExistingState = await RuntimeSettings.GetAsync(Key, (object?)null);

				if (ExistingState is T TypedValue)
					return TypedValue;

				return DefaultValueIfNotFound;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return DefaultValueIfNotFound;
		}

		/// <summary>
		/// Removes a given State.
		/// </summary>
		/// <param name="Key">The State identifier.</param>
		public async Task RemoveState(string Key)
		{
			AssertNonEmptyPRefix(Key);

			try
			{
				await RuntimeSettings.DeleteAsync(Key);
				await Database.Provider.Flush();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Waits for initialization of the storage service to be completed.
		/// </summary>
		/// <returns>If storage service is OK, or failed to initialize.</returns>
		public Task<bool> WaitInitDone()
		{
			return ServiceRef.StorageService.WaitInitDone();
		}
	}
}
