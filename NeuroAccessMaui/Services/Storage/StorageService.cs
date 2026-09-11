using System.Diagnostics;
using System.Text;
using Waher.Events;
using Waher.Events.Persistence;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Storage
{
	/// <summary>Owns application database initialization and shutdown.</summary>
	[Singleton]
	internal sealed class StorageService : IStorageService, IDisposableAsync
	{
        private readonly SemaphoreSlim lifecycleSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim keySemaphore = new SemaphoreSlim(1, 1);
        private readonly string dataFolder;
        private FilesProvider? databaseProvider;
        private PersistedEventLog? persistedEventLog;
        private Task? initialization;
        private Task? shutdown;
        private bool ownsProvider;

		/// <summary>
		/// Creates a new instance of the <see cref="StorageService"/> class.
		/// </summary>
		public StorageService()
		{
			string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.dataFolder = Path.Combine(AppDataFolder, "Data");

#if DEBUG && WINDOWS
            string name = Environment.GetEnvironmentVariable("NEUROACCESS_DATA_PROFILE") ?? "";
            this.dataFolder = Path.Combine(AppDataFolder, "Data" + name);
#endif
		}

		/// <summary>
		/// Folder for database.
		/// </summary>
		public string DataFolder => this.dataFolder;

        /// <inheritdoc />
        public bool HasExistingData() => this.GetExistingDataFiles().Count > 0;

        private HashSet<string> GetExistingDataFiles()
        {
            HashSet<string> Files = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            try { _ = File.GetAttributes(this.dataFolder); }
            catch (DirectoryNotFoundException) { return Files; }
            catch (FileNotFoundException) { return Files; }
            foreach (string FileName in Directory.EnumerateFiles(this.dataFolder, "*", SearchOption.AllDirectories))
                Files.Add(Path.GetFullPath(FileName));
            return Files;
        }

        /// <inheritdoc />
        public async Task Init(CancellationToken? cancellationToken)
        {
            Task Initialization;
            await this.lifecycleSemaphore.WaitAsync(cancellationToken ?? CancellationToken.None);
            try
            {
                if (this.shutdown is not null)
                    throw new ObjectDisposedException(nameof(StorageService));
                Initialization = this.initialization ??= this.InitializeAsync();
            }
            finally { this.lifecycleSemaphore.Release(); }
            await Initialization.WaitAsync(cancellationToken ?? CancellationToken.None);
        }

        private async Task InitializeAsync()
        {
            try
            {
                if (Database.HasProvider)
                {
                    this.databaseProvider = Database.Provider as FilesProvider
                        ?? throw new InvalidOperationException("An incompatible database provider is already registered.");
                }
                else
                {
                    HashSet<string> ExistingFiles = this.GetExistingDataFiles();
                    FilesProvider.AsyncFileIo = true;
                    this.databaseProvider = await FilesProvider.CreateAsync(this.dataFolder, "Default", 8192, 10000, 8192,
                        Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds,
                        FileName => this.GetDatabaseKeyAsync(FileName, ExistingFiles));
                    this.ownsProvider = true;
                    await this.databaseProvider.Start();
                    if (Database.HasProvider)
                        throw new InvalidOperationException("Another database provider was registered during initialization.");
                    Database.Register(this.databaseProvider, false);
                }
                this.persistedEventLog = new PersistedEventLog(90);
                Log.Register(this.persistedEventLog);
            }
            catch (Exception Ex)
            {
                try { await this.ReleaseAsync(false); }
                catch (Exception CleanupError) { Ex.Data["StorageCleanupFailure"] = CleanupError; }
                throw;
            }
        }

        private async Task<KeyValuePair<byte[], byte[]>> GetDatabaseKeyAsync(string FileName, HashSet<string> ExistingFiles)
        {
            await this.keySemaphore.WaitAsync();
            try
            {
                string FullPath = Path.GetFullPath(FileName);
                string Root = Path.GetFullPath(this.dataFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!FullPath.StartsWith(Root, Comparison))
                    throw new IOException("Database key path is outside the data directory.");
                // The provider allocates before this callback; only the pre-open snapshot is useful here.
                KeyValuePair<byte[], byte[]> Keys = await ServiceRef.CryptoService.GetCustomKeyAsync(FileName, !ExistingFiles.Contains(FullPath));
                ExistingFiles.Add(FullPath);
                return Keys;
            }
            finally { this.keySemaphore.Release(); }
        }

        /// <inheritdoc />
        public Task WaitForInitializationAsync() => this.initialization
            ?? Task.FromException(new InvalidOperationException("Storage initialization has not started."));

        /// <inheritdoc />
        public async Task Shutdown()
        {
            Task Shutdown;
            await this.lifecycleSemaphore.WaitAsync();
            try { Shutdown = this.shutdown ??= this.ShutdownAsync(); }
            finally { this.lifecycleSemaphore.Release(); }
            await Shutdown;
        }

        private async Task ShutdownAsync()
        {
            if (this.initialization is not null)
            {
                try { await this.initialization; }
                catch (Exception) { } // The initialization caller retains the original failure.
            }
            await this.ReleaseAsync(this.initialization?.IsCompletedSuccessfully == true);
        }

        private async Task ReleaseAsync(bool Flush)
        {
            List<Exception> Errors = new List<Exception>();
            if (this.persistedEventLog is not null)
            {
                PersistedEventLog EventLog = this.persistedEventLog;
                this.persistedEventLog = null;
                try { Log.Unregister(EventLog); }
                catch (Exception Ex) { Errors.Add(Ex); }
                try { await EventLog.DisposeAsync(); }
                catch (Exception Ex) { Errors.Add(Ex); }
            }
            if (this.databaseProvider is not null && this.ownsProvider)
            {
                FilesProvider Provider = this.databaseProvider;
                if (Flush)
                {
                    try { await Provider.Flush(); }
                    catch (Exception Ex) { Errors.Add(Ex); }
                }
                try
                {
                    if (Database.HasProvider && ReferenceEquals(Database.Provider, Provider))
                        Database.Register(new NullDatabaseProvider(), false);
                }
                catch (Exception Ex) { Errors.Add(Ex); }
                try { await Provider.DisposeAsync(); }
                catch (Exception Ex) { Errors.Add(Ex); }
            }
            this.databaseProvider = null;
            this.ownsProvider = false;
            if (Errors.Count > 0)
                throw new AggregateException("Storage cleanup failed.", Errors);
        }

        /// <summary>Synchronously releases owned storage resources.</summary>
        [Obsolete("Use DisposeAsync() instead.")]
        public void Dispose() => this.DisposeAsync().GetAwaiter().GetResult();

        /// <summary>Completes shutdown of owned storage resources.</summary>
        /// <returns>The retained shutdown outcome.</returns>
        public Task DisposeAsync() => this.Shutdown();


		/// <inheritdoc/>
		public async Task Insert(object obj)
		{
			await Database.Insert(obj);
			await Database.Provider.Flush();
		}

		/// <inheritdoc/>
		public async Task Update(object obj)
		{
			await Database.Update(obj);
			await Database.Provider.Flush();
		}

		/// <inheritdoc/>
		public Task<T> FindFirstDeleteRest<T>() where T : class
		{
			return Database.FindFirstDeleteRest<T>();
		}

		/// <inheritdoc/>
		public Task<T> FindFirstIgnoreRest<T>() where T : class
		{
			return Database.FindFirstIgnoreRest<T>();
		}

		/// <inheritdoc/>
		public Task Export(IDatabaseExport exportOutput)
		{
			return Database.Export(exportOutput);
		}

		/// <summary>
		/// Flags the database for repair, so that the next time the app is opened, the database will be repaired.
		/// </summary>
		public void FlagForRepair()
		{
			this.DeleteFile("Start.txt");
			this.DeleteFile("Stop.txt");
		}

		private void DeleteFile(string FileName)
		{
			try
			{
				FileName = Path.Combine(this.dataFolder, FileName);

				if (File.Exists(FileName))
					File.Delete(FileName);
			}
			catch (Exception)
			{
				// Ignore, to avoid infinite loops if event log has an inconsistency.
			}
		}
	}
}
