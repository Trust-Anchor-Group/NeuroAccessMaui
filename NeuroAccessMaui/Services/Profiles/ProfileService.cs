using System.Text;
using NeuroAccessMaui.Services.Contacts;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Avatar;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// Keeps the user's own card, and publishes what the user has chosen to share: the name and photo as a card on the user's
	/// account, and the photo for other phones, which fetch it when they see that it has changed.
	/// </summary>
	public static class ProfileService
	{
		private static readonly SemaphoreSlim saveLock = new(1, 1);
		private static MyProfile? cached;
		private static int started;

		/// <summary>
		/// Raised when the user's card has changed.
		/// </summary>
		public static event EventHandler? Changed;

		/// <summary>
		/// Publishes the card again each time the app connects. Calling it again does nothing.
		/// </summary>
		public static void Start()
		{
			if (Interlocked.Exchange(ref started, 1) == 1)
				return;

			AccountConnection.Connected += (Sender, e) => _ = PublishAsync();
		}

		/// <summary>
		/// Gets the user's card. The first time, the name comes from the user's ID, and nothing is shared.
		/// </summary>
		/// <returns>The card.</returns>
		public static async Task<MyProfile> GetAsync()
		{
			if (cached is not null)
				return cached;

			MyProfile? Stored = await Database.FindFirstIgnoreRest<MyProfile>();
			cached = Stored ?? new MyProfile()
			{
				Name = DefaultName()
			};

			return cached;
		}

		/// <summary>
		/// Saves the user's card, and publishes what is shared.
		/// </summary>
		/// <param name="Profile">The card.</param>
		public static async Task SaveAsync(MyProfile Profile)
		{
			await saveLock.WaitAsync();

			try
			{
				Profile.Updated = DateTime.UtcNow;

				if (string.IsNullOrEmpty(Profile.ObjectId))
					await Database.Insert(Profile);
				else
					await Database.Update(Profile);

				cached = Profile;
			}
			finally
			{
				saveLock.Release();
			}

			try
			{
				Changed?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			await PublishAsync();
		}

		/// <summary>
		/// Publishes what the user has chosen to share. What is not shared is taken away: the card on the account then has
		/// no name or no photo, and other phones get the plain default picture, so they stop showing the old photo.
		/// </summary>
		public static async Task PublishAsync()
		{
			if (!AccountConnection.IsConnected)
				return;

			try
			{
				MyProfile Profile = await GetAsync();
				bool SharesPhoto = Profile.SharePhoto && Profile.Photo is not null;

				await AccountConnection.RequestOwnAccountAsync(true, CardXml(Profile, SharesPhoto));

				AvatarClient? Avatars = AccountConnection.Avatars;
				if (Avatars is not null)
				{
					Avatar Default = Avatars.DefaultAvatar;
					byte[] Photo = SharesPhoto ? Profile.Photo! : Default.Binary;
					string ContentType = SharesPhoto ? ProfilePhotos.ContentType : Default.ContentType;
					int Side = SharesPhoto ? ProfilePhotos.Size : Default.Width;

					if (!string.Equals(Avatars.LocalAvatar.Hash, Hashes.ComputeSHA1HashString(Photo), StringComparison.OrdinalIgnoreCase))
					{
						await Avatars.UpdateLocalAvatarAsync(ContentType, Photo, Side, Side, false);
						Avatars.IncludeAvatarInPresence = true;
						await AccountConnection.ResendPresenceAsync();
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// The card on the user's account, with only what is shared.
		/// </summary>
		private static string CardXml(MyProfile Profile, bool SharesPhoto)
		{
			StringBuilder Xml = new();

			Xml.Append("<vCard xmlns='vcard-temp'>");

			string Name = Profile.Name.Trim();
			if (Profile.ShareName && Name.Length > 0)
			{
				string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				string Family = Words.Length > 1 ? Words[^1] : string.Empty;
				string Given = Words.Length > 1 ? string.Join(' ', Words[..^1]) : Name;

				Xml.Append("<FN>");
				Xml.Append(XML.Encode(Name));
				Xml.Append("</FN><N><FAMILY>");
				Xml.Append(XML.Encode(Family));
				Xml.Append("</FAMILY><GIVEN>");
				Xml.Append(XML.Encode(Given));
				Xml.Append("</GIVEN></N>");
			}

			if (SharesPhoto)
			{
				Xml.Append("<PHOTO><TYPE>");
				Xml.Append(ProfilePhotos.ContentType);
				Xml.Append("</TYPE><BINVAL>");
				Xml.Append(Convert.ToBase64String(Profile.Photo!));
				Xml.Append("</BINVAL></PHOTO>");
			}

			Xml.Append("</vCard>");
			return Xml.ToString();
		}

		/// <summary>
		/// The user's name from their ID, if they have one.
		/// </summary>
		private static string DefaultName()
		{
			Waher.Networking.XMPP.Contracts.LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			if (Identity is null)
				return string.Empty;

			return ContactInfo.GetFullName(Identity[Constants.XmppProperties.FirstName] ?? string.Empty,
				Identity[Constants.XmppProperties.MiddleNames] ?? string.Empty,
				Identity[Constants.XmppProperties.LastNames] ?? string.Empty);
		}
	}
}
