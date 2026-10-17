using EDaler;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroFeatures;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events.Persistence;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Avatar;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.Geo;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.Mail;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.PEP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Geo;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Runtime.Settings.SettingObjects;
using Waher.Script;
using Waher.Script.Content;
using Waher.Script.Graphs;
using Waher.Security;
using Waher.Security.EllipticCurves;
using Waher.Security.JWS;
using Waher.Security.JWT;
using Waher.Things;

namespace NeuroAccessMaui.Services.Startup
{
	/// <summary>
	/// Builds the Waher runtime type inventory on the thread pool.
	/// </summary>
	/// <remarks>
	/// <see cref="Types.Initialize(System.Reflection.Assembly[])"/> reflects over every type in about forty assemblies. Running
	/// it in the application constructor blocked the UI thread on every process start, including starts triggered by push
	/// messages. The inventory is process-global in Waher, which is why this coordinator is static as well.
	/// Services are registered in dependency injection and do not depend on the inventory. Code that does use it, such as
	/// persistence, scripting, content encoding or <c>Types.FindBest</c>,
	/// awaits <see cref="WaitUntilReadyAsync"/> first. Nothing waits for the inventory synchronously.
	/// </remarks>
	public static class RuntimeTypeInventory
	{
		private static readonly object syncObject = new object();
		private static Task? initialization;

		/// <summary>
		/// Gets a value indicating whether the inventory has been built.
		/// </summary>
		public static bool IsReady => Types.IsInitialized;

		/// <summary>
		/// Starts building the inventory on the thread pool, unless it is already built or being built.
		/// </summary>
		/// <remarks>
		/// Safe to call any number of times from any thread. A failed attempt is started again.
		/// </remarks>
		public static void EnsureStarted()
		{
			_ = GetOrStartInitialization();
		}

		/// <summary>
		/// Waits until the inventory has been built, starting the build if necessary.
		/// </summary>
		/// <returns>A task that completes when the inventory is ready, or faults if building it failed.</returns>
		public static Task WaitUntilReadyAsync()
		{
			if (Types.IsInitialized)
				return Task.CompletedTask;

			return GetOrStartInitialization();
		}

		private static Task GetOrStartInitialization()
		{
			lock (syncObject)
			{
				if (initialization is null || initialization.IsFaulted || initialization.IsCanceled)
					initialization = Task.Run(Initialize);

				return initialization;
			}
		}

		private static void Initialize()
		{
			if (Types.IsInitialized)
				return;

			Types.Initialize(
				typeof(App).Assembly,
				typeof(Database).Assembly,
				typeof(ObjectSerializer).Assembly,
				typeof(FilesProvider).Assembly,
				typeof(Setting).Assembly,
				typeof(RuntimeSettings).Assembly,
				typeof(PersistedEvent).Assembly,
				typeof(InternetContent).Assembly,
				typeof(ImageCodec).Assembly,
				typeof(MarkdownDocument).Assembly,
				typeof(XML).Assembly,
				typeof(DnsResolver).Assembly,
				typeof(XmppClient).Assembly,
				typeof(ContractsClient).Assembly,
				typeof(NeuroFeaturesClient).Assembly,
				typeof(EDalerClient).Assembly,
				typeof(SensorClient).Assembly,
				typeof(ControlClient).Assembly,
				typeof(ConcentratorClient).Assembly,
				typeof(ProvisioningClient).Assembly,
				typeof(PubSubClient).Assembly,
				typeof(PepClient).Assembly,
				typeof(AvatarClient).Assembly,
				typeof(PushNotificationClient).Assembly,
				typeof(MailClient).Assembly,
				typeof(GeoClient).Assembly,
				typeof(GeoPosition).Assembly,
				typeof(ThingReference).Assembly,
				typeof(JwtFactory).Assembly,
				typeof(JwsAlgorithm).Assembly,
				typeof(Expression).Assembly,
				typeof(Graph).Assembly,
				typeof(GraphEncoder).Assembly,
				typeof(XmppServerlessMessaging).Assembly,
				typeof(HttpxClient).Assembly,
				typeof(Waher.Script.Persistence.SQL.Select).Assembly,
				typeof(Waher.Script.Xml.XmlParser).Assembly,
				typeof(Waher.Layout.Layout2D.Layout2DDocument).Assembly,
				typeof(IPaceProtocol).Assembly,
				typeof(ISignatureAlgorithm).Assembly,
				typeof(EllipticCurve).Assembly);
		}
	}
}
