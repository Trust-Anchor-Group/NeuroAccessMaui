using System.Globalization;
using System.Text;
using System.Xml;
using NeuroAccessMaui.Services.Contacts;
using Waher.Content.Xml;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Settings;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// The opt-in backup of the user's contacts, kept as one contact file in the account's private storage on the server
	/// (XEP-0049). Photos are left out. It is off until the user turns it on, and then follows every change to the contacts.
	/// A restore imports the file like any contact file, so restored contacts are never marked as verified.
	/// </summary>
	public static class ContactBackup
	{
		/// <summary>
		/// The namespace of the backup element in private storage.
		/// </summary>
		public const string Namespace = "urn:neuroaccess:contacts:backup:1";

		private const string privateStorageNamespace = "jabber:iq:private";
		private const string elementName = "contacts";
		private const string enabledSetting = "CONTACTS.BACKUP_ENABLED";
		private const string lastSetting = "CONTACTS.BACKUP_LAST";
		private const string changedSetting = "CONTACTS.BACKUP_CHANGED";
		private const string restoreAskedSetting = "CONTACTS.RESTORE_ASKED";

		private static readonly TimeSpan backupDelay = TimeSpan.FromSeconds(30);
		private static readonly SemaphoreSlim backupLock = new(1, 1);
		private static CancellationTokenSource? pendingBackup;
		private static int started;

		/// <summary>
		/// Raised when a backup has been made, restored or deleted, or the switch has changed.
		/// </summary>
		public static event EventHandler? Changed;

		/// <summary>
		/// Starts following changes to the contacts and new connections. Calling it again does nothing.
		/// </summary>
		public static void Start()
		{
			if (Interlocked.Exchange(ref started, 1) == 1)
				return;

			ContactStore.Changed += ContactStore_Changed;
			AccountConnection.Connected += AccountConnection_Connected;
		}

		#region Settings

		/// <summary>
		/// If the user has turned the backup on.
		/// </summary>
		/// <returns>If the backup is on.</returns>
		public static Task<bool> IsEnabledAsync()
		{
			return RuntimeSettings.GetAsync(enabledSetting, false);
		}

		/// <summary>
		/// Turns the backup on or off. Turning it on backs up the contacts at once.
		/// </summary>
		/// <param name="Enabled">If the backup is on.</param>
		public static async Task SetEnabledAsync(bool Enabled)
		{
			await RuntimeSettings.SetAsync(enabledSetting, Enabled);

			if (Enabled)
			{
				// If the app is offline now, the backup is made when it connects.
				await RuntimeSettings.SetAsync(changedSetting, true);
				await BackupAsync();
			}
			else
				RaiseChanged();
		}

		/// <summary>
		/// When the contacts were last backed up from this phone.
		/// </summary>
		/// <returns>The time in UTC, or <see cref="DateTime.MinValue"/> if they have not been.</returns>
		public static Task<DateTime> GetLastBackupAsync()
		{
			return RuntimeSettings.GetAsync(lastSetting, DateTime.MinValue);
		}

		#endregion

		#region Backup, restore and delete

		/// <summary>
		/// Backs up all contacts now, as one contact file without photos.
		/// </summary>
		/// <returns>How many contacts were backed up.</returns>
		/// <exception cref="InvalidOperationException">If the app is not connected.</exception>
		public static async Task<int> BackupAsync()
		{
			await backupLock.WaitAsync();

			try
			{
				IReadOnlyList<Contact> Contacts = await ContactStore.GetAllAsync();
				string Text = ContactStore.Export(Contacts, false, true);
				DateTime Now = DateTime.UtcNow;
				StringBuilder Xml = new();

				Xml.Append("<query xmlns='");
				Xml.Append(privateStorageNamespace);
				Xml.Append("'><");
				Xml.Append(elementName);
				Xml.Append(" xmlns='");
				Xml.Append(Namespace);
				Xml.Append("' updated='");
				Xml.Append(XML.Encode(Now));
				Xml.Append("' count='");
				Xml.Append(Contacts.Count.ToString(CultureInfo.InvariantCulture));
				Xml.Append("'>");
				Xml.Append(XML.Encode(Text));
				Xml.Append("</");
				Xml.Append(elementName);
				Xml.Append("></query>");

				await AccountConnection.RequestOwnAccountAsync(true, Xml.ToString());

				await RuntimeSettings.SetAsync(lastSetting, Now);
				await RuntimeSettings.SetAsync(changedSetting, false);

				ServiceRef.LogService.LogInformational("ContactsBackedUp", new KeyValuePair<string, object?>("Count", Contacts.Count));
				RaiseChanged();

				return Contacts.Count;
			}
			finally
			{
				backupLock.Release();
			}
		}

		/// <summary>
		/// Reads what the backup holds. If there is no backup, says so instead of failing.
		/// </summary>
		/// <returns>What the backup holds.</returns>
		/// <exception cref="InvalidOperationException">If the app is not connected.</exception>
		public static async Task<ContactBackupInfo> GetInfoAsync()
		{
			XmlElement? Answer;

			try
			{
				Answer = await AccountConnection.RequestOwnAccountAsync(false, "<query xmlns='" + privateStorageNamespace + "'><" + elementName +
					" xmlns='" + Namespace + "'/></query>");
			}
			catch (ItemNotFoundException)
			{
				return ContactBackupInfo.None;
			}

			if (Answer is null)
				return ContactBackupInfo.None;

			foreach (XmlNode N in Answer.ChildNodes)
			{
				if (N is not XmlElement E || E.LocalName != elementName || E.NamespaceURI != Namespace)
					continue;

				string Text = E.InnerText;
				if (string.IsNullOrWhiteSpace(Text))
					return ContactBackupInfo.None;

				int Count = XML.Attribute(E, "count", 0);
				DateTime Updated = XML.TryParse(XML.Attribute(E, "updated"), out DateTime Parsed) ? Parsed.ToUniversalTime() : DateTime.MinValue;

				return new ContactBackupInfo(true, Count, Updated, Text);
			}

			return ContactBackupInfo.None;
		}

		/// <summary>
		/// Imports the contacts in the backup. Contacts already on the phone get what the backup adds; the others are added.
		/// They are never marked as verified.
		/// </summary>
		/// <returns>How many contacts were added or changed.</returns>
		/// <exception cref="InvalidOperationException">If the app is not connected.</exception>
		public static async Task<int> RestoreAsync()
		{
			ContactBackupInfo Info = await GetInfoAsync();
			if (!Info.Exists)
				return 0;

			ContactImportPreview Preview = await ContactStore.PreviewImportAsync(Info.Text);
			List<ContactImportItem> Items = [.. Preview.Items.Where(Item => Item.Kind is ContactImportKind.New or ContactImportKind.Update)];

			int Count = Items.Count == 0 ? 0 : await ContactStore.ImportAsync(Items);

			await RuntimeSettings.SetAsync(restoreAskedSetting, true);
			ServiceRef.LogService.LogInformational("ContactsRestored", new KeyValuePair<string, object?>("Count", Count));
			RaiseChanged();

			return Count;
		}

		/// <summary>
		/// Deletes the backup from the account's private storage, and turns the backup off.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the app is not connected.</exception>
		public static async Task DeleteAsync()
		{
			await backupLock.WaitAsync();

			try
			{
				pendingBackup?.Cancel();

				// An element with no content and no attributes deletes the stored one.
				await AccountConnection.RequestOwnAccountAsync(true, "<query xmlns='" + privateStorageNamespace + "'><" + elementName +
					" xmlns='" + Namespace + "'/></query>");

				await RuntimeSettings.SetAsync(enabledSetting, false);
				await RuntimeSettings.SetAsync(lastSetting, DateTime.MinValue);
			}
			finally
			{
				backupLock.Release();
			}

			RaiseChanged();
		}

		/// <summary>
		/// If the user should be offered to restore the backup: the phone has no contacts, there is a backup, and the user
		/// has not been asked before. Never fails.
		/// </summary>
		/// <returns>If a restore should be offered.</returns>
		public static async Task<bool> ShouldOfferRestoreAsync()
		{
			try
			{
				if (await RuntimeSettings.GetAsync(restoreAskedSetting, false) || !AccountConnection.IsConnected)
					return false;

				if ((await ContactStore.GetAllAsync()).Count > 0)
					return false;

				return (await GetInfoAsync()).Exists;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return false;
			}
		}

		/// <summary>
		/// Notes that the user has been asked to restore the backup, so they are not asked again.
		/// </summary>
		public static Task MarkRestoreOfferedAsync()
		{
			return RuntimeSettings.SetAsync(restoreAskedSetting, true);
		}

		#endregion

		#region Following changes

		private static void ContactStore_Changed(object? Sender, EventArgs e)
		{
			_ = OnContactsChangedAsync();
		}

		/// <summary>
		/// Notes that the contacts have changed, and backs them up a while after the last change, if the backup is on.
		/// </summary>
		private static async Task OnContactsChangedAsync()
		{
			try
			{
				if (!await IsEnabledAsync())
					return;

				await RuntimeSettings.SetAsync(changedSetting, true);

				pendingBackup?.Cancel();
				CancellationTokenSource Delay = new();
				pendingBackup = Delay;

				await Task.Delay(backupDelay, Delay.Token);
				await BackupIfChangedAsync();
			}
			catch (TaskCanceledException)
			{
				// A later change backs up instead.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static void AccountConnection_Connected(object? Sender, EventArgs e)
		{
			_ = Task.Run(BackupIfChangedAsync);
		}

		/// <summary>
		/// Backs up the contacts if the backup is on, the app is connected, and the contacts have changed since the last
		/// backup.
		/// </summary>
		private static async Task BackupIfChangedAsync()
		{
			try
			{
				if (!AccountConnection.IsConnected || !await IsEnabledAsync() || !await RuntimeSettings.GetAsync(changedSetting, false))
					return;

				await BackupAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

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
