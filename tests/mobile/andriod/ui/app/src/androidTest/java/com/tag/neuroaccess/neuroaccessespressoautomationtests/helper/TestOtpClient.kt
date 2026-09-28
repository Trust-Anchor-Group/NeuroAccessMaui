package com.tag.neuroaccess.neuroaccessespressoautomationtests.helper

import android.os.SystemClock
import androidx.test.platform.app.InstrumentationRegistry
import java.io.IOException
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder
import java.nio.charset.StandardCharsets
import java.util.concurrent.Callable
import java.util.concurrent.ExecutionException
import java.util.concurrent.FutureTask
import java.util.concurrent.TimeUnit
import java.util.concurrent.TimeoutException
import javax.net.ssl.SSLException

/** Retrieves a verification code within a bounded time without exposing endpoint or phone data. */
object TestOtpClient {
    private const val ENDPOINT_ARGUMENT = "testOtpEndpoint"
    private const val MAXIMUM_ATTEMPTS = 15
    private const val TOTAL_TIMEOUT_MILLISECONDS = 30_000L
    private const val REQUEST_TIMEOUT_MILLISECONDS = 5_000L
    private const val RETRY_DELAY_MILLISECONDS = 1_000L
    private const val MAXIMUM_RESPONSE_CHARACTERS = 1_048_576

    private val codePatterns = listOf(
        Regex("""(?is)(?:verification\s*code|otp(?:\s*code)?|code)\D{0,240}?(?<!\d)(\d{6})(?!\d)""")
    )

    /**
     * Requests the current verification code with a total deadline covering all network operations.
     * @param phoneNumber The phone number in E.164 format.
     * @return The verification code displayed by the configured TestOTP endpoint.
     */
    fun fetchVerificationCode(phoneNumber: String): String {
        require(phoneNumber.startsWith("+")) { "Phone number must be supplied in E.164 format." }
        val endpoint = this.endpoint()
        val deadline = SystemClock.elapsedRealtime() + TOTAL_TIMEOUT_MILLISECONDS
        // HttpURLConnection timeouts do not bound DNS, writes or a slowly streaming response.
        val request = FutureTask(Callable { this.fetchWithinDeadline(endpoint, phoneNumber, deadline) })
        val worker = Thread(request, "test-otp-request").apply { isDaemon = true }
        worker.start()
        try {
            return request.get(this.remainingMilliseconds(deadline), TimeUnit.MILLISECONDS)
        } catch (_: TimeoutException) {
            throw IllegalStateException("TestOTP exceeded its 30-second total deadline.")
        } catch (_: InterruptedException) {
            Thread.currentThread().interrupt()
            throw IllegalStateException("TestOTP retrieval was interrupted.")
        } catch (failure: ExecutionException) {
            // Only propagate messages constructed here, never network exception details.
            throw IllegalStateException(
                when (val cause = failure.cause) {
                    is TimeoutException -> "TestOTP exceeded its 30-second total deadline."
                    is OtpFailure -> cause.message
                    else -> "TestOTP request failed."
                }
            )
        } finally {
            request.cancel(true)
        }
    }

    private fun fetchWithinDeadline(endpoint: URL, phoneNumber: String, deadline: Long): String {
        var lastFailure = "No verification code was present in the response."
        repeat(MAXIMUM_ATTEMPTS) { attempt ->
            this.remainingMilliseconds(deadline)
            try {
                val body = this.postPhoneNumber(endpoint, phoneNumber, deadline)
                if (body != null) {
                    this.findVerificationCode(body)?.let { return it }
                    lastFailure = "No verification code was present in the response."
                } else {
                    lastFailure = "The endpoint returned a retryable HTTP status."
                }
            } catch (_: SSLException) {
                throw OtpFailure("TestOTP TLS negotiation failed.")
            } catch (_: IOException) {
                lastFailure = "A network request failed or timed out."
            }
            if (attempt < MAXIMUM_ATTEMPTS - 1) {
                Thread.sleep(minOf(RETRY_DELAY_MILLISECONDS, this.remainingMilliseconds(deadline)))
            }
        }
        throw OtpFailure("TestOTP exhausted $MAXIMUM_ATTEMPTS attempts. $lastFailure")
    }

    private fun postPhoneNumber(endpoint: URL, phoneNumber: String, deadline: Long): String? {
        val connection = endpoint.openConnection() as HttpURLConnection
        val requestBody = ("PhoneNr=" + URLEncoder.encode(phoneNumber, StandardCharsets.UTF_8.name()))
            .toByteArray(StandardCharsets.UTF_8)
        try {
            connection.connectTimeout = minOf(REQUEST_TIMEOUT_MILLISECONDS, this.remainingMilliseconds(deadline)).toInt()
            connection.readTimeout = connection.connectTimeout
            connection.requestMethod = "POST"
            connection.instanceFollowRedirects = true
            connection.doOutput = true
            connection.setRequestProperty("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")
            connection.setRequestProperty("Accept", "text/html")
            connection.outputStream.use { it.write(requestBody) }

            this.remainingMilliseconds(deadline)
            val status = connection.responseCode
            if (status == 408 || status == 429 || status in 500..599) {
                return null
            }
            if (status !in 200..299) {
                throw OtpFailure("TestOTP returned non-retryable HTTP $status.")
            }
            return connection.inputStream.bufferedReader(StandardCharsets.UTF_8).use { reader ->
                val body = StringBuilder()
                val buffer = CharArray(4096)
                while (true) {
                    this.remainingMilliseconds(deadline)
                    val count = reader.read(buffer)
                    if (count < 0) break
                    if (body.length + count > MAXIMUM_RESPONSE_CHARACTERS) {
                        throw OtpFailure("TestOTP response exceeded the allowed size.")
                    }
                    body.append(buffer, 0, count)
                }
                body.toString()
            }
        } finally {
            connection.disconnect()
        }
    }

    private fun remainingMilliseconds(deadline: Long): Long {
        if (Thread.currentThread().isInterrupted) throw InterruptedException()
        val remaining = deadline - SystemClock.elapsedRealtime()
        if (remaining <= 0) throw TimeoutException()
        return remaining
    }

    private fun endpoint(): URL {
        val configuredEndpoint = InstrumentationRegistry.getArguments()
            .getString(ENDPOINT_ARGUMENT)?.takeIf(String::isNotBlank)
            ?: throw IllegalStateException("Missing instrumentation argument '$ENDPOINT_ARGUMENT'.")
        return try {
            URL(configuredEndpoint).also {
                require(it.protocol == "http" || it.protocol == "https")
            }
        } catch (_: Exception) {
            throw IllegalStateException("TestOTP endpoint must be a valid HTTP or HTTPS URL.")
        }
    }

    private fun findVerificationCode(responseBody: String): String? {
        for (pattern in codePatterns) {
            pattern.find(responseBody)?.groupValues?.getOrNull(1)?.let { return it }
        }
        return null
    }

    /** Contains only diagnostic messages that are safe to include in test reports. */
    private class OtpFailure(message: String) : RuntimeException(message)
}
