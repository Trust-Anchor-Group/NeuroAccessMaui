namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Contains a prepared value, or the reason preparation could not finish.
	/// </summary>
	/// <typeparam name="T">The type of value required by the caller.</typeparam>
	public sealed class PreparationResult<T> where T : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PreparationResult{T}"/> class.
		/// </summary>
		/// <param name="Status">The preparation outcome.</param>
		/// <param name="Value">The prepared value. It must be provided exactly when <paramref name="Status"/> is <see cref="PreparationStatus.Ready"/>.</param>
		/// <exception cref="ArgumentException">Thrown when the presence of <paramref name="Value"/> does not match <paramref name="Status"/>.</exception>
		public PreparationResult(PreparationStatus Status, T? Value = null)
		{
			if ((Status == PreparationStatus.Ready) != (Value is not null))
				throw new ArgumentException("Only a ready result may contain a value, and it must contain one.", nameof(Value));

			this.Status = Status;
			this.Value = Value;
		}

		/// <summary>
		/// Gets the preparation outcome.
		/// </summary>
		public PreparationStatus Status { get; }

		/// <summary>
		/// Gets the prepared value when <see cref="Status"/> is <see cref="PreparationStatus.Ready"/>; otherwise <c>null</c>.
		/// </summary>
		public T? Value { get; }
	}
}
