package com.tag.neuroaccess.neuroaccessespressoautomationtests.quicklogin

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData

import android.annotation.SuppressLint
import android.os.SystemClock
import android.util.Log
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.test.platform.app.InstrumentationRegistry
import org.json.JSONObject
import org.json.JSONTokener
import java.io.Closeable
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

/** Keeps the real Quick Login page and its event connection alive while the app signs in. */
class QuickLoginWebSession : Closeable {
    private val instrumentation = InstrumentationRegistry.getInstrumentation()
    private lateinit var webView: WebView

    /** Loads a fresh browser session without replacing the app's visible activity. */
    @SuppressLint("SetJavaScriptEnabled")
    fun open() {
        check(!this::webView.isInitialized) { "Quick Login page is already open." }
        val pageUrl = TestData.quickLoginPageUrl()
        Log.i("QuickLoginTest", "WEB: loading configured Quick Login page (URL omitted)")
        this.instrumentation.runOnMainSync {
            this.webView = WebView(this.instrumentation.targetContext)
            this.webView.settings.javaScriptEnabled = true
            this.webView.settings.domStorageEnabled = true
            this.webView.webViewClient = WebViewClient()
            this.webView.loadUrl(pageUrl)
        }
    }

    /** @return The current page's QR link, sharing its TabID and live event connection. */
    fun awaitSignUrl(): String = this.awaitValue("the page's QR link") {
        val link = this.evaluate("document.getElementById('quickLoginA')?.getAttribute('href') || ''")
        link.takeIf { it.startsWith("tagsign:") && it.substringAfter(',').isNotBlank() }
    }.also { Log.i("QuickLoginTest", "WEB: QR link read from #quickLoginA (payload omitted)") }

    /** @return The purpose that the app must display before accepting the request. */
    fun purpose(): String = this.evaluate(
        "document.getElementById('quickLoginCode')?.getAttribute('data-purpose') || ''"
    ).also { check(it.isNotBlank()) { "Quick Login page has no request purpose." } }

    /**
     * Waits for the page's own success handler and verifies the rendered identity table.
     * @param expectedIdentityId Optional known identity ID for an exact comparison.
     */
    fun awaitApprovedIdentity(expectedIdentityId: String? = null) {
        Log.i("QuickLoginTest", "WEB: waiting for Successfully logged in. and Identity of user.")
        val rows = JSONObject(this.awaitValue("the successful login table") {
            this.evaluate("""
                (() => {
                    const panel = document.getElementById('quickLoginCode');
                    if (!panel || panel.getAttribute('data-done') !== '1' ||
                        !Array.from(panel.querySelectorAll('h2')).some(h => h.textContent.trim() === 'Successfully logged in.')) return '';
                    const table = Array.from(panel.querySelectorAll('table')).find(table =>
                        table.querySelector('thead th')?.textContent.trim() === 'Identity of user.');
                    if (!table) return '';
                    const rows = {};
                    table.querySelectorAll('tbody tr').forEach(row => {
                        const cells = row.querySelectorAll('td');
                        if (cells.length === 2) rows[cells[0].textContent.trim()] = cells[1].textContent.trim();
                    });
                    return JSON.stringify(rows);
                })()
            """.trimIndent()).takeIf { it.isNotBlank() }
        })
        val identityId = rows.optString("Id")
        Log.i("QuickLoginTest", "WEB: Identity of user. / Id = $identityId")
        Log.i("QuickLoginTest", "WEB: Identity of user. / State = ${rows.optString("State")}")
        check(identityId.contains('@') && identityId.substringBefore('@').isNotBlank() &&
            identityId.substringAfter('@').isNotBlank()) {
            "Quick Login returned an invalid identity ID."
        }
        if (expectedIdentityId != null) {
            if (identityId != expectedIdentityId) {
                Log.e("QuickLoginTest", "FAIL: mobile Id = $expectedIdentityId; web Id = $identityId; IDs differ")
            }
            check(identityId == expectedIdentityId) { "Quick Login returned a different identity." }
        }
        check(rows.optString("State") == "Approved") { "Quick Login identity is not Approved." }
        check(rows.optString("Provider") == identityId.substringAfter('@')) {
            "Quick Login returned an unexpected identity provider."
        }
        for (field in listOf("Created", "From", "To", "Client Key Name", "Client Public Key", "Client Signature", "Server Signature")) {
            check(rows.optString(field).isNotBlank()) { "Quick Login identity table is missing $field." }
        }
        if (expectedIdentityId != null) {
            Log.i("QuickLoginTest", "PASS: mobile Neuro-ID equals web Id; State=Approved; required fields present")
        }
    }

    private fun awaitValue(description: String, read: () -> String?): String {
        val deadline = SystemClock.elapsedRealtime() + 60_000L
        do {
            read()?.let { return it }
            Thread.sleep(200)
        } while (SystemClock.elapsedRealtime() < deadline)
        Log.e("QuickLoginTest", "FAIL: timed out waiting for $description; web verification did not complete")
        error("Timed out waiting for $description.")
    }

    private fun evaluate(script: String): String {
        val completed = CountDownLatch(1)
        var result = "null"
        this.instrumentation.runOnMainSync {
            this.webView.evaluateJavascript(script) {
                result = it
                completed.countDown()
            }
        }
        check(completed.await(5, TimeUnit.SECONDS)) { "Quick Login page did not respond." }
        return JSONTokener(result).nextValue() as? String ?: ""
    }

    /** Closes the page and its event connection even when the test fails. */
    override fun close() {
        if (this::webView.isInitialized) this.instrumentation.runOnMainSync {
            this.webView.stopLoading()
            this.webView.destroy()
        }
    }
}
