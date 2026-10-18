using System.Net;
using System.Net.Sockets;
using System.Text;
using NeuroAccessMaui.Testing.Services;

namespace NeuroAccessMaui.IntegrationTests.Services;

/// <summary>Exercises the OTP client's real HTTP requests and responses against a local test endpoint.</summary>
[TestClass, TestCategory("Integration")]
public sealed class TestOtpClientTests
{
	/// <summary>Preserves the form field, phone encoding, Accept header and HTML code extraction.</summary>
	/// <returns>A task representing the protocol check.</returns>
	[TestMethod]
	public async Task Test_01_PostEncodedPhoneAndReadHtmlCodeAsync()
	{
		List<string> Requests = await RunServerAsync([(200, "<html>Verification code: <b>492816</b></html>")], async Endpoint =>
		{
			Assert.AreEqual("492816", await new TestOtpClient(Endpoint).FetchAsync("+15550001234"));
		});
		Assert.HasCount(1, Requests);
		Assert.StartsWith(Requests[0], "POST /otp HTTP/1.1\n");
		Assert.Contains(Requests[0], "Accept: text/html");
		Assert.Contains(Requests[0], "application/x-www-form-urlencoded");
		Assert.EndsWith(Requests[0], "PhoneNr=%2B15550001234");
	}

	/// <summary>Retries transient status codes and returns the subsequent response's code.</summary>
	/// <param name="Status">The transient HTTP status sent on the first attempt.</param>
	/// <returns>A task representing the retry check.</returns>
	[TestMethod, DataRow(408), DataRow(429), DataRow(503)]
	public async Task Test_02_RetryTransientResponseAsync(int Status)
	{
		List<string> Requests = await RunServerAsync([(Status, "retry"), (200, "OTP code: 492816")], async Endpoint =>
		{
			Assert.AreEqual("492816", await new TestOtpClient(Endpoint).FetchAsync("+15550001234"));
		});
		Assert.HasCount(2, Requests);
	}

	/// <summary>Stops on a permanent response without echoing the endpoint, phone or response body.</summary>
	/// <returns>A task representing the redaction check.</returns>
	[TestMethod]
	public async Task Test_03_RejectPermanentResponseWithoutLeakingDataAsync()
	{
		List<string> Requests = await RunServerAsync([(401, "private-server-detail 492816")], async Endpoint =>
		{
			AssertFailedException Error = await Assert.ThrowsExactlyAsync<AssertFailedException>(
				() => new TestOtpClient(Endpoint).FetchAsync("+15550001234"));
			Assert.Contains(Error.Message, "OTP endpoint returned non-retryable HTTP 401.");
			Assert.IsFalse(Error.Message.Contains("private-server-detail", StringComparison.Ordinal));
			Assert.IsFalse(Error.Message.Contains("492816", StringComparison.Ordinal));
			Assert.IsFalse(Error.Message.Contains("+15550001234", StringComparison.Ordinal));
			Assert.IsFalse(Error.Message.Contains(Endpoint.AbsoluteUri, StringComparison.Ordinal));
		});
		Assert.HasCount(1, Requests);
	}

	/// <summary>Bounds a real response stream before attempting to parse its content.</summary>
	/// <returns>A task representing the response size check.</returns>
	[TestMethod]
	public async Task Test_04_RejectOversizedResponseAsync()
	{
		await RunServerAsync([(200, new string('x', 1_048_577))], async Endpoint =>
		{
			AssertFailedException Error = await Assert.ThrowsExactlyAsync<AssertFailedException>(
				() => new TestOtpClient(Endpoint).FetchAsync("+15550001234"));
			Assert.Contains(Error.Message, "OTP response exceeds the size limit.");
		});
	}

	/// <summary>Polls again when the successful HTTP response does not contain a six-digit OTP.</summary>
	/// <returns>A task representing delayed code availability.</returns>
	[TestMethod]
	public async Task Test_05_RetryUntilCodeBecomesAvailableAsync()
	{
		List<string> Requests = await RunServerAsync([(200, "code: 1234567"), (200, "verification code: 492816")], async Endpoint =>
		{
			Assert.AreEqual("492816", await new TestOtpClient(Endpoint).FetchAsync("+15550001234"));
		});
		Assert.HasCount(2, Requests);
	}

	private static async Task<List<string>> RunServerAsync((int Status, string Body)[] Responses, Func<Uri, Task> Scenario)
	{
		using CancellationTokenSource Deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		using TcpListener Listener = new TcpListener(IPAddress.Loopback, 0);
		Listener.Start();
		int Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
		List<string> Requests = new List<string>();
		Task Server = ServeAsync(Listener, Responses, Requests, Deadline.Token);
		try
		{
			await Scenario(new Uri($"http://127.0.0.1:{Port}/otp"));
			await Server;
			return Requests;
		}
		finally
		{
			await Deadline.CancelAsync();
			Listener.Stop();
			try { await Server; }
			catch (OperationCanceledException) { }
		}
	}

	private static async Task ServeAsync(TcpListener Listener, (int Status, string Body)[] Responses,
		List<string> Requests, CancellationToken CancellationToken)
	{
		foreach ((int Status, string Body) in Responses)
		{
			using TcpClient Connection = await Listener.AcceptTcpClientAsync(CancellationToken);
			using NetworkStream Stream = Connection.GetStream();
			using StreamReader Reader = new StreamReader(Stream, Encoding.UTF8, leaveOpen: true);
			StringBuilder Request = new StringBuilder();
			int ContentLength = 0;
			string? Line;
			while (!string.IsNullOrEmpty(Line = await Reader.ReadLineAsync(CancellationToken)))
			{
				Request.Append(Line).Append('\n');
				if (Line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
					ContentLength = int.Parse(Line[15..].Trim(), System.Globalization.CultureInfo.InvariantCulture);
			}
			char[] Form = new char[ContentLength];
			Assert.AreEqual(ContentLength, await Reader.ReadBlockAsync(Form.AsMemory(), CancellationToken));
			Requests.Add(Request.Append('\n').Append(Form).ToString());
			byte[] Content = Encoding.UTF8.GetBytes(Body);
			byte[] Headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {Status} Test\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {Content.Length}\r\nConnection: close\r\n\r\n");
			await Stream.WriteAsync(Headers, CancellationToken);
			await Stream.WriteAsync(Content, CancellationToken);
		}
	}
}
