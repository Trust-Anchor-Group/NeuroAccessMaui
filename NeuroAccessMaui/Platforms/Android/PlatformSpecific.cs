﻿using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Views;
using Waher.Events;
using Android.App;

namespace NeuroAccessMaui.Services;

public class PlatformSpecific : IPlatformSpecific
{
	public PlatformSpecific()
	{
	}

	private static bool screenProtected = true; // App started with screen protected.
	private static Timer? protectionTimer = null;

	/// <inheritdoc/>
	public bool CanProhibitScreenCapture => true;

	/// <inheritdoc/>
	public bool ProhibitScreenCapture
	{
		get => screenProtected;

		set
		{
			try
			{
				protectionTimer?.Dispose();
				protectionTimer = null;

				if (screenProtected != value)
				{
					this.SetScreenSecurityProtection(value);
					screenProtected = value;
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}
	}

	private void SetScreenSecurityProtection(bool enabled)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			try
			{
				Android.App.Activity? activity = Platform.CurrentActivity;

				if (activity is not null)
				{
					if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
					{
#pragma warning disable CA1416
						activity.SetRecentsScreenshotEnabled(!enabled);
#pragma warning restore CA1416
					}

					if (enabled)
					{
						activity.Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
						protectionTimer = new Timer(this.ProtectionTimerElapsed, null, 1000 * 60 * 60, Timeout.Infinite);
					}
					else
					{
						activity.Window?.ClearFlags(WindowManagerFlags.Secure);
					}
				}
			}
			catch (Exception)
			{
			}
		});
	}

	private void ProtectionTimerElapsed(object? P)
	{
		MainThread.BeginInvokeOnMainThread(() => this.ProhibitScreenCapture = false);
	}

	/// <inheritdoc/>
	public string? GetDeviceId()
	{
		Android.Content.ContentResolver? ContentResolver = Android.App.Application.Context.ContentResolver;

		if (ContentResolver is not null)
		{
			return Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
		}

		return null;
	}

	/// <inheritdoc/>
	public Task CloseApplication()
	{
		Android.App.Activity? Activity = Android.App.Application.Context as Android.App.Activity;    // TODO: returns null. Context points to Application instance.
		Activity?.FinishAffinity();

		Java.Lang.JavaSystem.Exit(0);

		return Task.CompletedTask;
	}

	public async Task<byte[]> CaptureScreen(int blurRadius)
	{
		blurRadius = Math.Min(25, Math.Max(blurRadius, 0));

		Activity? activity = Platform.CurrentActivity;
		var rootView = activity.Window.DecorView.RootView;

		using (var screenshot = Bitmap.CreateBitmap(
							rootView.Width,
							rootView.Height,
							Bitmap.Config.Argb8888))
		{
			var canvas = new Canvas(screenshot);
			rootView.Draw(canvas);

			Bitmap Blurred = null;

			if (activity != null && (int)Android.OS.Build.VERSION.SdkInt >= 17)
			{
				Blurred = ToBlurred(screenshot, activity, blurRadius);
			}
			else
			{
				Blurred = ToLegacyBlurred(screenshot, blurRadius);
			}

			{
				MemoryStream Stream = new MemoryStream();
				Blurred.Compress(Bitmap.CompressFormat.Jpeg, 80, Stream);
				Stream.Seek(0, SeekOrigin.Begin);

				return Stream.ToArray();
			}
		}
	}

	private Bitmap ToBlurred(Bitmap originalBitmap, Activity? Activity, int radius)
	{
		// Create another bitmap that will hold the results of the filter.
		Bitmap blurredBitmap = Bitmap.CreateBitmap(originalBitmap);
		RenderScript renderScript = RenderScript.Create(Activity);

		// Load up an instance of the specific script that we want to use.
		// An Element is similar to a C type. The second parameter, Element.U8_4,
		// tells the Allocation is made up of 4 fields of 8 unsigned bits.
		ScriptIntrinsicBlur script = ScriptIntrinsicBlur.Create(renderScript, Android.Renderscripts.Element.U8_4(renderScript));

		// Create an Allocation for the kernel inputs.
		Allocation input = Allocation.CreateFromBitmap(renderScript, originalBitmap,
													   Allocation.MipmapControl.MipmapFull,
													   AllocationUsage.Script);

		// Assign the input Allocation to the script.
		script.SetInput(input);

		// Set the blur radius
		script.SetRadius(radius);

		// Finally we need to create an output allocation to hold the output of the Renderscript.
		Allocation output = Allocation.CreateTyped(renderScript, input.Type);

		// Next, run the script. This will run the script over each Element in the Allocation, and copy it's
		// output to the allocation we just created for this purpose.
		script.ForEach(output);

		// Copy the output to the blurred bitmap
		output.CopyTo(blurredBitmap);

		// Cleanup.
		output.Destroy();
		input.Destroy();
		script.Destroy();
		renderScript.Destroy();

		return blurredBitmap;
	}

	// Source: http://incubator.quasimondo.com/processing/superfast_blur.php
	public static Bitmap ToLegacyBlurred(Bitmap source, int radius)
	{
		var config = source.GetConfig();

		if (config == null)
		{
			config = Bitmap.Config.Argb8888;    // This will support transparency
		}

		Bitmap img = source.Copy(config, true);

		int w = img.Width;
		int h = img.Height;
		int wm = w - 1;
		int hm = h - 1;
		int wh = w * h;
		int div = radius + radius + 1;
		int[] r = new int[wh];
		int[] g = new int[wh];
		int[] b = new int[wh];
		int rsum, gsum, bsum, x, y, i, p, p1, p2, yp, yi, yw;
		int[] vmin = new int[Math.Max(w, h)];
		int[] vmax = new int[Math.Max(w, h)];
		int[] pix = new int[w * h];

		img.GetPixels(pix, 0, w, 0, 0, w, h);

		int[] dv = new int[256 * div];
		for (i = 0; i < 256 * div; i++)
		{
			dv[i] = (i / div);
		}

		yw = yi = 0;

		for (y = 0; y < h; y++)
		{
			rsum = gsum = bsum = 0;
			for (i = -radius; i <= radius; i++)
			{
				p = pix[yi + Math.Min(wm, Math.Max(i, 0))];
				rsum += (p & 0xff0000) >> 16;
				gsum += (p & 0x00ff00) >> 8;
				bsum += p & 0x0000ff;
			}
			for (x = 0; x < w; x++)
			{

				r[yi] = dv[rsum];
				g[yi] = dv[gsum];
				b[yi] = dv[bsum];

				if (y == 0)
				{
					vmin[x] = Math.Min(x + radius + 1, wm);
					vmax[x] = Math.Max(x - radius, 0);
				}
				p1 = pix[yw + vmin[x]];
				p2 = pix[yw + vmax[x]];

				rsum += ((p1 & 0xff0000) - (p2 & 0xff0000)) >> 16;
				gsum += ((p1 & 0x00ff00) - (p2 & 0x00ff00)) >> 8;
				bsum += (p1 & 0x0000ff) - (p2 & 0x0000ff);
				yi++;
			}
			yw += w;
		}

		for (x = 0; x < w; x++)
		{
			rsum = gsum = bsum = 0;
			yp = -radius * w;
			for (i = -radius; i <= radius; i++)
			{
				yi = Math.Max(0, yp) + x;
				rsum += r[yi];
				gsum += g[yi];
				bsum += b[yi];
				yp += w;
			}
			yi = x;
			for (y = 0; y < h; y++)
			{
				// Preserve alpha channel: ( 0xff000000 & pix[yi] )
				var rgb = (dv[rsum] << 16) | (dv[gsum] << 8) | dv[bsum];
				pix[yi] = ((int)(0xff000000 & pix[yi]) | rgb);
				if (x == 0)
				{
					vmin[y] = Math.Min(y + radius + 1, hm) * w;
					vmax[y] = Math.Max(y - radius, 0) * w;
				}
				p1 = x + vmin[y];
				p2 = x + vmax[y];

				rsum += r[p1] - r[p2];
				gsum += g[p1] - g[p2];
				bsum += b[p1] - b[p2];

				yi += w;
			}
		}

		img.SetPixels(pix, 0, w, 0, 0, w, h);
		return img;
	}
}