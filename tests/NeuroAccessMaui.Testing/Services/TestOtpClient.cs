using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Testing.Services;

/// <summary>Retrieves the verification code sent to a temporary test phone number.</summary>
internal sealed class TestOtpClient
{
	private const int attempts = 15;
	private readonly Uri endpoint;

	/// <summary>Creates a client for the test OTP endpoint.</summary>
	internal TestOtpClient(Uri Endpoint) => this.endpoint = Endpoint;

	/// <summary>Polls the endpoint until it returns a six-digit code for <paramref name="Phone"/>.</summary>
	internal async Task<string> FetchAsync(string Phone)
	{
		using HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
		for (int i = 0; i < attempts; i++)
		{
			try
			{
				using FormUrlEncodedContent Form = new FormUrlEncodedContent(new Dictionary<string, string> { ["PhoneNr"] = Phone });
				using HttpResponseMessage Response = await Client.PostAsync(this.endpoint, Form);
				if (Response.IsSuccessStatusCode)
				{
					string Content = await Response.Content.ReadAsStringAsync();
					Match Code = Regex.Match(Content, @"(?is)(?:verification\s*code|otp(?:\s*code)?|code)\D{0,240}?(?<!\d)(\d{6})(?!\d)");
					if (Code.Success)
						return Code.Groups[1].Value;
				}
			}
			catch (Exception Error) when (Error is HttpRequestException or TaskCanceledException)
			{
				// The endpoint is temporary test infrastructure; retry until the attempts run out.
			}
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
		throw new AssertFailedException($"No verification code for {Phone} at {this.endpoint} after {attempts} attempts.");
	}
}
