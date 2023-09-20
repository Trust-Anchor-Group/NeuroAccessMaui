namespace NeuroAccessMaui.Services;

/// <inheritdoc/>
public class LoadableService : ServiceReferences, ILoadableService
{
	//private SemaphoreSlim worker = new SemaphoreSlim(1, 1);

	/// <summary>
	/// Gets whether the service is loaded.
	/// </summary>
	public bool IsLoaded { get; protected set; }

	/// <summary>
	/// Gets whether the service is being loaded.
	/// </summary>
	public bool IsLoading { get; protected set; }

	/// <summary>
	/// Gets whether the service is being unloaded.
	/// </summary>
	public bool IsUnloading { get; protected set; }

	/// <summary>
	/// Sets the <see cref="IsLoading"/> flag if the service isn't already loading.
	/// </summary>
	/// <returns><c>true</c> if the service will load, <c>false</c> otherwise.</returns>
	protected bool BeginLoad(CancellationToken cancellationToken)
	{
		//this.worker.Wait();

		if (this.IsLoaded)
		{
			//this.worker.Release();
			return false;
		}

		this.IsLoading = true;

		cancellationToken.ThrowIfCancellationRequested();

		return true;
	}

	/// <summary>
	/// Sets the <see cref="IsLoading"/> and <see cref="IsLoaded"/> flags and fires an event
	/// representing the current load state of the service.
	/// </summary>
	/// <param name="isLoaded">The current loaded state to set.</param>
	protected void EndLoad(bool isLoaded)
	{
		this.IsLoaded = isLoaded;
		this.IsLoading = false;

		//this.worker.Release();

		this.OnLoaded(new LoadedEventArgs(this.IsLoaded));
	}

	/// <summary>
	/// Sets the <see cref="IsLoading"/> flag if the service isn't already unloading.
	/// </summary>
	/// <returns><c>true</c> if the service will unload, <c>false</c> otherwise.</returns>
	protected bool BeginUnload()
	{
		//this.worker.Wait();

		if (!this.IsLoaded)
		{
			//this.worker.Release();
			return false;
		}

		this.IsUnloading = true;

		return true;
	}

	/// <summary>
	/// Sets the <see cref="IsLoading"/> and <see cref="IsLoaded"/> flags and fires an event
	/// representing the current load state of the service.
	/// </summary>
	protected void EndUnload()
	{
		this.IsLoaded = false;
		this.IsUnloading = false;

		//this.worker.Release();

		this.OnLoaded(new LoadedEventArgs(this.IsLoaded));
	}

	/// <inheritdoc/>
	public virtual Task Load(bool isResuming, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public virtual Task Unload()
	{
		return Task.CompletedTask;
	}

	private event EventHandler<LoadedEventArgs> PrivLoaded;

	/// <inheritdoc/>
	public event EventHandler<LoadedEventArgs> Loaded
	{
		add
		{
			PrivLoaded += value;
			value(this, new LoadedEventArgs(this.IsLoaded));
		}
		remove
		{
			PrivLoaded -= value;
		}
	}

	private void OnLoaded(LoadedEventArgs e)
	{
		PrivLoaded?.Invoke(this, e);
	}
}
