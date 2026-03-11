using System;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Implements the Generic Mapping PACE algorithm.
	/// </summary>
	public static class GenericMapping
	{
		/// <summary>
		/// Authenticates the application with the document, using the PACE protocol.
		/// </summary>
		/// <param name="IsoDep">NFC interface for communicating with the document.</param>
		/// <param name="DocInfo">Document information.</param>
		/// <param name="Protocol">Cipher suite selected.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		public static async Task<bool> Authenticate(IIsoDepInterface IsoDep, DocumentInformation DocInfo,
			PaceEcdhProtocol Protocol)
		{
			if (Protocol?.Curve is null)
				return false;

			try
			{
				// Encrypted Nonce

				byte[]? z = await IsoDep.GetPaceEncryptedNonce();
				if (z is null)
				{
					IsoDep.Error("Unable to get PACE encrypted nonce.");
					return false;
				}

				IsoDep.Information("Encrypted nonce: " + Hashes.BinaryToString(z));

				byte[] Kπ = Protocol.Kπ(DocInfo);
				byte[] s = Protocol.DecryptNonce(Kπ, z);

				IsoDep.Information("Decrypted nonce: " + Hashes.BinaryToString(s));

				// Main keys

				byte[] LocalPublicKey = Protocol.CreateNewKey();
				byte[]? RemotePublicKey = await IsoDep.GetPaceRemotePublicKey(LocalPublicKey);

				if (RemotePublicKey is null)
				{
					IsoDep.Error("Unable to get PACE remote public key.");
					return false;
				}

				// Shared Secret

				byte[] SharedSecret = Protocol.GetSharedSecret(RemotePublicKey);

				IsoDep.Information("Shared secret: " + Hashes.BinaryToString(SharedSecret));

				// Map

				PointOnCurve Ĝ = Protocol.GetGenericMap(s, RemotePublicKey);
				byte[] Generator = Protocol.Curve!.Encode(Ĝ, true);

				// Ephemeral keys

				byte[] LocalEphemeralPrivateKey = Protocol.Curve.GenerateSecret();
				PointOnCurve P1 = Protocol.Curve.ScalarMultiplication(LocalEphemeralPrivateKey, Ĝ, true);
				byte[] LocalEphemeralPublicKey = Protocol.Curve.Encode(P1, true);

				byte[]? RemoteEphemeralPublicKey = await IsoDep.GetPaceRemotePublicEphemeralKey(LocalEphemeralPublicKey);

				if (RemoteEphemeralPublicKey is null)
				{
					IsoDep.Error("Unable to get PACE remote ephemeral public key.");
					return false;
				}

				// Ephemeral shared secret

				int c = RemoteEphemeralPublicKey.Length;
				int c2 = c >> 1;
				byte[] RemoteEphemeralPublicKeyX = new byte[c2];
				byte[] RemoteEphemeralPublicKeyY = new byte[c2];

				Buffer.BlockCopy(RemoteEphemeralPublicKey, 0, RemoteEphemeralPublicKeyX, 0, c2);
				Buffer.BlockCopy(RemoteEphemeralPublicKey, c2, RemoteEphemeralPublicKeyY, 0, c2);

				Array.Reverse(RemoteEphemeralPublicKeyX);
				Array.Reverse(RemoteEphemeralPublicKeyY);

				PointOnCurve RemoteEphemeralPublicPoint = new(
					EllipticCurve.ToInt(RemoteEphemeralPublicKeyX),
					EllipticCurve.ToInt(RemoteEphemeralPublicKeyY));

				PointOnCurve EphemeralSharedPoint = Protocol.Curve.ScalarMultiplication(
					LocalEphemeralPrivateKey, RemoteEphemeralPublicPoint, true);

				byte[] EphemeralSharedPointX = EphemeralSharedPoint.X.ToByteArray();

				if (EphemeralSharedPointX.Length != Protocol.Curve.OrderBytes)
					Array.Resize(ref EphemeralSharedPointX, Protocol.Curve.OrderBytes);

				Array.Reverse(EphemeralSharedPointX);

				// Session keys

				byte[] KS_Enc = PaceProtocol.KDF_Enc(EphemeralSharedPointX, false);
				byte[] KS_Mac = PaceProtocol.KDF_Mac(EphemeralSharedPointX, false);

				// Associated Data

				byte[] AD_IFD = PaceProtocol.CreateAssociatedData(Protocol.Oid, RemoteEphemeralPublicKey);
				byte[] AD_IC = PaceProtocol.CreateAssociatedData(Protocol.Oid, LocalEphemeralPublicKey);

				// Computing MAC

				CMac Mac = Protocol.GetAuthenticator(KS_Mac);

				byte[] T_IFD = Mac.Sign(AD_IFD, 8);
				byte[] T_IC = Mac.Sign(AD_IC, 8);

				byte[]? RemoteToken = await IsoDep.GetPaceRemoteVerificationToken(T_IFD);
				if (RemoteToken is null || RemoteToken.Length != T_IC.Length ||
					Convert.ToBase64String(T_IC) != Convert.ToBase64String(RemoteToken))
				{
					IsoDep.Error("PACE token validation failed.");
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				IsoDep.Exception(ex);
				return false;
			}
		}
	}
}
