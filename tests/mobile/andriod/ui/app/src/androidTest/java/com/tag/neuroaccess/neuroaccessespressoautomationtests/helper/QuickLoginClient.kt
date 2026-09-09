package com.tag.neuroaccess.neuroaccessespressoautomationtests.helper

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData

import android.os.Looper
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URI
import java.net.URL
import java.util.concurrent.Callable
import java.util.concurrent.ExecutionException
import java.util.concurrent.FutureTask
import java.util.concurrent.TimeUnit
import java.util.concurrent.TimeoutException

/** Retrieves fresh Quick Login QR payloads using the same API as the web page. */
object QuickLoginClient {
    /**
     * Creates a login request and returns its QR payload for manual entry in the app.
     * An empty tab creates an independent request; use the page's TabID to target that page.
     * @param endpoint The QuickLogin API URL, not the Markdown page URL.
     * @param tabId The receiving browser tab identifier, or empty for an independent request.
     * @param serviceId Optional registered backend service identifier.
     * @return A fresh tagsign URI. Treat it as temporary and do not log it.
     */
    fun fetchSignUrl(
        endpoint: String = TestData.quickLoginEndpoint(),
        tabId: String = "",
        serviceId: String = ""
    ): String {
        check(Looper.myLooper() != Looper.getMainLooper()) {
            "Quick Login must be requested from the instrumentation thread."
        }
        val url = try {
            URL(endpoint).also {
                require(it.protocol == "https" && it.host.isNotBlank() && it.userInfo == null)
            }
        } catch (_: Exception) {
            error("Quick Login endpoint must be a valid HTTPS URL without credentials.")
        }
        val request = FutureTask(Callable { this.requestSignUrl(url, tabId, serviceId) })
        Thread(request, "quick-login-request").apply { isDaemon = true }.start()
        try {
            return request.get(30, TimeUnit.SECONDS)
        } catch (_: TimeoutException) {
            error("Quick Login exceeded its 30-second deadline.")
        } catch (_: InterruptedException) {
            Thread.currentThread().interrupt()
            error("Quick Login retrieval was interrupted.")
        } catch (_: ExecutionException) {
            // Network and parser errors can include the temporary login payload.
            error("Quick Login failed to return a valid signUrl.")
        } finally {
            request.cancel(true)
        }
    }

    private fun requestSignUrl(endpoint: URL, tabId: String, serviceId: String): String {
        val connection = endpoint.openConnection() as HttpURLConnection
        try {
            connection.connectTimeout = 5_000
            connection.readTimeout = 5_000
            connection.requestMethod = "POST"
            connection.instanceFollowRedirects = false
            connection.doOutput = true
            connection.setRequestProperty("Content-Type", "application/json; charset=UTF-8")
            connection.setRequestProperty("Accept", "application/json")
            val payload = JSONObject()
                .put("serviceId", serviceId)
                .put("tab", tabId)
                .put("mode", "image")
                .put("purpose", "To test Quick Login with NeuroAccess using manual QR entry.")
                .toString().toByteArray(Charsets.UTF_8)
            connection.setFixedLengthStreamingMode(payload.size)
            connection.outputStream.use { it.write(payload) }
            check(connection.responseCode == HttpURLConnection.HTTP_OK)
            val body = connection.inputStream.bufferedReader(Charsets.UTF_8).use { reader ->
                val result = StringBuilder()
                val buffer = CharArray(4096)
                while (true) {
                    if (Thread.currentThread().isInterrupted) throw InterruptedException()
                    val count = reader.read(buffer)
                    if (count < 0) break
                    check(result.length + count <= 1_048_576)
                    result.append(buffer, 0, count)
                }
                result.toString()
            }
            val signUrl = JSONObject(body).getString("signUrl")
            val uri = URI(signUrl)
            check(uri.scheme.equals("tagsign", ignoreCase = true))
            check(uri.schemeSpecificPart.contains(',') && uri.schemeSpecificPart.substringAfter(',').isNotBlank())
            return signUrl
        } finally {
            connection.disconnect()
        }
    }
}
