using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// Keeps the groups on the phone: the ones the user is in, has left, or is invited to. The addresses are also kept in
	/// memory, so code that sends and shows messages can tell a group from a person without reading the database.
	/// </summary>
	public static class GroupStore
	{
		private static readonly HashSet<string> addresses = new(StringComparer.OrdinalIgnoreCase);
		private static readonly SemaphoreSlim saveLock = new(1, 1);
		private static int loaded;

		/// <summary>
		/// Raised when a group has been added, changed or removed.
		/// </summary>
		public static event EventHandler? Changed;

		/// <summary>
		/// Reads the addresses of the groups into memory. Calling it again does nothing.
		/// </summary>
		public static async Task LoadAsync()
		{
			if (Interlocked.Exchange(ref loaded, 1) == 1)
				return;

			try
			{
				foreach (ChatGroup Group in await Database.Find<ChatGroup>())
				{
					lock (addresses)
					{
						addresses.Add(Group.Address.Value);
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// If a chat address is a group's.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <returns>If it is a group's address.</returns>
		public static bool IsGroup(string? Address)
		{
			if (string.IsNullOrEmpty(Address))
				return false;

			lock (addresses)
			{
				return addresses.Contains(Address);
			}
		}

		/// <summary>
		/// Gets all groups on the phone.
		/// </summary>
		/// <returns>The groups.</returns>
		public static async Task<IReadOnlyList<ChatGroup>> GetAllAsync()
		{
			IEnumerable<ChatGroup> All = await Database.Find<ChatGroup>();
			return [.. All];
		}

		/// <summary>
		/// Gets the groups the user is invited to and has not answered yet, newest first.
		/// </summary>
		/// <returns>The invites.</returns>
		public static async Task<IReadOnlyList<ChatGroup>> GetInvitesAsync()
		{
			IEnumerable<ChatGroup> All = await Database.Find<ChatGroup>(new FilterFieldEqualTo("Status", GroupStatus.Invited));
			return [.. All.OrderByDescending(Group => Group.Created)];
		}

		/// <summary>
		/// Finds a group by its address.
		/// </summary>
		/// <param name="Address">The group's address.</param>
		/// <returns>The group, if it is on the phone.</returns>
		public static async Task<ChatGroup?> FindAsync(string? Address)
		{
			if (string.IsNullOrEmpty(Address))
				return null;

			return await Database.FindFirstIgnoreRest<ChatGroup>(new FilterFieldEqualTo("Address", Address));
		}

		/// <summary>
		/// Gets the groups the user is in together with a person.
		/// </summary>
		/// <param name="Address">The person's chat address.</param>
		/// <returns>The groups.</returns>
		public static async Task<IReadOnlyList<ChatGroup>> GetSharedWithAsync(string Address)
		{
			List<ChatGroup> Result = [];

			foreach (ChatGroup Group in await Database.Find<ChatGroup>(new FilterFieldEqualTo("Status", GroupStatus.Joined)))
			{
				if (Group.Members?.Any(Member => string.Equals(Member.Address, Address, StringComparison.OrdinalIgnoreCase)) ?? false)
					Result.Add(Group);
			}

			return Result;
		}

		/// <summary>
		/// Saves a group, new or changed.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Notify">If <see cref="Changed"/> is raised. Bookkeeping that nothing shows, such as when the latest
		/// message came, needs no update of the pages.</param>
		public static async Task SaveAsync(ChatGroup Group, bool Notify = true)
		{
			await saveLock.WaitAsync();

			try
			{
				if (string.IsNullOrEmpty(Group.ObjectId))
				{
					if (Group.Created == DateTime.MinValue)
						Group.Created = DateTime.UtcNow;

					await Database.Insert(Group);
				}
				else
					await Database.Update(Group);

				lock (addresses)
				{
					addresses.Add(Group.Address.Value);
				}
			}
			finally
			{
				saveLock.Release();
			}

			if (Notify)
				RaiseChanged();
		}

		/// <summary>
		/// Removes a group from the phone. Its messages are not deleted here.
		/// </summary>
		/// <param name="Group">The group.</param>
		public static async Task DeleteAsync(ChatGroup Group)
		{
			if (!string.IsNullOrEmpty(Group.ObjectId))
				await Database.Delete(Group);

			lock (addresses)
			{
				addresses.Remove(Group.Address.Value);
			}

			RaiseChanged();
		}

		private static void RaiseChanged()
		{
			try
			{
				Changed?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
