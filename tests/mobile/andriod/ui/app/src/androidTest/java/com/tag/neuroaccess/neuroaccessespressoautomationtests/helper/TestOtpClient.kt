package com.tag.neuroaccess.neuroaccessespressoautomationtests.helper

import android.os.SystemClock
import androidx.test.platform.app.InstrumentationRegistry
import java.io.BufferedReader
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder
import java.nio.charset.StandardCharsets

/** Retrieves a verification code from the configured TestOTP endpoint for end-to-end Android tests. */
object TestOtpClient {
    private const val ENDPOINT_ARGUMENT = "testOtpEndpoint"
    private const val MAXIMUM_ATTEMPTS = 15
    private const val RETRY_DELAY_MILLISECONDS = 1_000L

    private val codePatterns = listOf(
        Regex(
            """(?is)(?:verification\s*code|otp(?:\s*code)?|code)\D{0,240}?(?<!\d)(\d{6})(?!\d)"""
        )
    )

    /**
     * Requests and returns the current verification code for a phone number.
     *
     * @param phoneNumber The phone number in E.164 format, using E.164 format.
     * @return The verification code displayed by the configured TestOTP endpoint.
     */
    fun fetchVerificationCode(phoneNumber: String): String {
        require(phoneNumber.startsWith("+")) {
            "Phone number must be supplied in E.164 format."
        }

        repeat(MAXIMUM_ATTEMPTS) { attempt ->
            val responseBody = this.postPhoneNumber(phoneNumber)
            val verificationCode = this.findVerificationCode(responseBody)

            if (verificationCode != null) {
                return verificationCode
            }

            if (attempt < MAXIMUM_ATTEMPTS - 1) {
                SystemClock.sleep(RETRY_DELAY_MILLISECONDS)
            }
        }

        throw IllegalStateException(
            "No verification code was found for the supplied test phone number."
        )
    }

    private fun postPhoneNumber(phoneNumber: String): String {
        val connection = URL(this.endpoint()).openConnection() as HttpURLConnection
        val requestBody = "PhoneNr=" + URLEncoder.encode(phoneNumber, StandardCharsets.UTF_8.toString())

        try {
            connection.requestMethod = "POST"
            connection.instanceFollowRedirects = true
            connection.doOutput = true
            connection.setRequestProperty(
                "Content-Type",
                "application/x-www-form-urlencoded; charset=UTF-8"
            )
            connection.setRequestProperty("Accept", "text/html")

            OutputStreamWriter(connection.outputStream, StandardCharsets.UTF_8).use { writer ->
                writer.write(requestBody)
            }

            val responseCode = connection.responseCode
            val responseStream = if (responseCode in 200..399) {
                connection.inputStream
            } else {
                connection.errorStream
                    ?: throw IllegalStateException("TestOTP returned HTTP $responseCode.")
            }

            return BufferedReader(responseStream.reader(StandardCharsets.UTF_8)).use { reader ->
                reader.readText()
            }
        } finally {
            connection.disconnect()
        }
    }

    private fun endpoint(): String =
        InstrumentationRegistry.getArguments()
            .getString(ENDPOINT_ARGUMENT)
            ?.takeIf(String::isNotBlank)
            ?: throw IllegalStateException(
                "Missing instrumentation argument '$ENDPOINT_ARGUMENT'."
            )

    private fun findVerificationCode(responseBody: String): String? {
        for (pattern in codePatterns) {
            val match = pattern.find(responseBody)

            if (match != null) {
                return match.groupValues.getOrNull(1)
            }
        }

        return null
    }
}
