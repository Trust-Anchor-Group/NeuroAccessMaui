using Waher.Networking.XMPP.Avatar;
using Waher.Security;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// The photos of people, as files on the phone the pages can show: the user's own photo, a photo on a contact's card, or
	/// the photo a contact's phone shares. Where there is none, the pages show initials.
	/// </summary>
	public static class PhotoCache
	{
		private static readonly Dictionary<string, string?> pathByAddress = new(StringComparer.OrdinalIgnoreCase);
		private static AvatarClient? attached;

		/// <summary>
		/// Raised when a person's photo has changed. The argument is their chat address.
		/// </summary>
		public static event EventHandler<string>? Changed;

		/// <summary>
		/// Starts following the photos other phones share.
		/// </summary>
		/// <param name="Avatars">The client that exchanges photos with other phones.</param>
		public static void Attach(AvatarClient Avatars)
		{
			Detach();

			Avatars.AvatarAdded += Avatars_Changed;
			Avatars.AvatarUpdated += Avatars_Changed;
			Avatars.AvatarRemoved += Avatars_Changed;
			attached = Avatars;

			ProfileService.Changed += ProfileService_Changed;
		}

		/// <summary>
		/// Stops following the photos other phones share, when the connection is closed.
		/// </summary>
		public static void Detach()
		{
			AvatarClient? Old = attached;
			attached = null;

			if (Old is not null)
			{
				Old.AvatarAdded -= Avatars_Changed;
				Old.AvatarUpdated -= Avatars_Changed;
				Old.AvatarRemoved -= Avatars_Changed;
			}

			ProfileService.Changed -= ProfileService_Changed;
		}

		/// <summary>
		/// Gets the photo of a person, as a file.
		/// </summary>
		/// <param name="Address">The person's chat address, or the user's own for their own photo.</param>
		/// <returns>The file, or null if there is no photo.</returns>
		public static async Task<string?> GetPathAsync(string? Address)
		{
			if (string.IsNullOrEmpty(Address))
				return null;

			lock (pathByAddress)
			{
				if (pathByAddress.TryGetValue(Address, out string? Known))
					return Known;
			}

			string? Path = null;

			try
			{
				if (string.Equals(Address, AccountConnection.OwnAddress ?? ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase))
				{
					MyProfile Profile = await ProfileService.GetAsync();
					if (Profile.Photo is not null)
						Path = await StoreAsync(Profile.Photo);
				}
				else if (AccountConnection.Avatars is AvatarClient Avatars)
				{
					Avatar? Found = await Avatars.GetAvatarAsync(Address);

					if (Found?.Binary is not null && Found.Binary.Length > 0 &&
						!string.Equals(Found.Hash, Avatars.DefaultAvatar.Hash, StringComparison.OrdinalIgnoreCase))
					{
						Path = await StoreAsync(Found.Binary);
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			lock (pathByAddress)
			{
				pathByAddress[Address] = Path;
			}

			return Path;
		}

		/// <summary>
		/// Gets the photo of a person, to show.
		/// </summary>
		/// <param name="Address">The person's chat address, or the user's own for their own photo.</param>
		/// <returns>The photo, or null if there is none.</returns>
		public static async Task<ImageSource?> GetAsync(string? Address)
		{
			string? Path = await GetPathAsync(Address);
			return Path is null ? null : ImageSource.FromFile(Path);
		}

		/// <summary>
		/// Stores a picture as a file named by its contents, so the same picture is stored once.
		/// </summary>
		/// <param name="Data">The picture.</param>
		/// <returns>The file.</returns>
		public static async Task<string> StoreAsync(byte[] Data)
		{
			string Folder = System.IO.Path.Combine(FileSystem.CacheDirectory, "Photos");
			Directory.CreateDirectory(Folder);

			string File = System.IO.Path.Combine(Folder, Hashes.ComputeSHA1HashString(Data) + ".img");
			if (!System.IO.File.Exists(File))
				await System.IO.File.WriteAllBytesAsync(File, Data);

			return File;
		}

		private static Task Avatars_Changed(object? Sender, AvatarEventArgs e)
		{
			Forget(e.BareJid);
			return Task.CompletedTask;
		}

		private static void ProfileService_Changed(object? Sender, EventArgs e)
		{
			string? Own = AccountConnection.OwnAddress ?? ServiceRef.XmppService.BareJid;
			if (!string.IsNullOrEmpty(Own))
				Forget(Own);
		}

		/// <summary>
		/// Forgets a person's photo, so it is read again, and tells the pages.
		/// </summary>
		private static void Forget(string Address)
		{
			lock (pathByAddress)
			{
				pathByAddress.Remove(Address);
			}

			try
			{
				Changed?.Invoke(null, Address);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
