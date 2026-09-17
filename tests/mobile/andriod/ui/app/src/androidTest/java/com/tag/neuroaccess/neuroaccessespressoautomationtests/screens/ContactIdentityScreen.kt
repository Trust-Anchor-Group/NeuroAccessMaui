package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.graphics.Rect
import android.net.Uri
import android.os.SystemClock
import android.view.InputDevice
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import android.view.accessibility.AccessibilityNodeInfo
import android.widget.ImageView
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher

/** Interacts with the animated ID card without waiting for Espresso animation idleness. */
object ContactIdentityScreen {
    private val instrumentation = InstrumentationRegistry.getInstrumentation()

    /** Waits for a visible English UI label without waiting for animation idleness. */
    fun waitForText(text: String) {
        awaitValue("visible label '$text'") { textNode(text) }
    }

    /** Taps a visible English UI label, including native dialogs. */
    fun tapText(text: String) {
        val bounds = awaitValue("visible label '$text'") { textNode(text)?.let(::nodeBounds) }
        tap(bounds)
    }

    /** Taps the first visible label whose text starts with the supplied prefix. */
    fun tapTextStartingWith(prefix: String) {
        val bounds = awaitValue("visible label starting with '$prefix'") {
            nodes(instrumentation.uiAutomation.rootInActiveWindow)
                .firstOrNull {
                    it.isVisibleToUser && it.text?.toString()?.startsWith(prefix) == true &&
                        !nodeBounds(it).isEmpty
                }
                ?.let(::nodeBounds)
        }
        tap(bounds)
    }
    /** Reports whether an exact visible label is present in the active window. */
    fun isTextVisible(text: String): Boolean = textNode(text) != null
    /** Copies the actual card QR payload and reads the foreground app's clipboard. */
    fun copyQrLink(): String {
        val clipboard = instrumentation.targetContext.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        instrumentation.runOnMainSync { clipboard.setPrimaryClip(ClipData.newPlainText("", "")) }
        val qr = awaitValue("a unique QR image on the lower half of the ID card") {
            var bounds: Rect? = null
            instrumentation.runOnMainSync {
                val screen = ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
                    .asSequence().flatMap { views(it.window.decorView) }
                    .firstOrNull { AutomationIdMatcher.matches(it, ViewIdentityScreen.SCREEN) && it.isShown }
                if (screen != null) {
                    val screenBounds = Rect()
                    screen.getGlobalVisibleRect(screenBounds)
                    // The card has no QR AutomationId: require one square ImageView below its midpoint.
                    val images = views(screen).filterIsInstance<ImageView>().mapNotNull { image ->
                        val rect = Rect()
                        rect.takeIf {
                            image.drawable != null && image.isShown && image.getGlobalVisibleRect(it) &&
                                it.width() > screenBounds.width() / 8 &&
                                it.centerY() > screenBounds.centerY() &&
                                kotlin.math.abs(it.width() - it.height()) < it.width() / 5
                        }
                    }.toList()
                    bounds = images.singleOrNull()
                }
            }
            bounds
        }
        tap(qr)
        awaitValue("clipboard success dialog") {
            textNode("A link to the ID was copied to the clipboard.")
        }
        val link = awaitValue("fresh ID clipboard content") {
            var value: String? = null
            instrumentation.runOnMainSync {
                value = clipboard.primaryClip?.takeIf { it.itemCount > 0 }
                    ?.getItemAt(0)?.text?.toString()?.takeIf { it.isNotBlank() }
            }
            value
        }
        check(!Uri.parse(link).scheme.isNullOrBlank() && link.none { it.isWhitespace() }) {
            "Clipboard does not contain an ID URI."
        }
        tapText("OK")
        return link
    }

    /** Ensures that the displayed identity is saved as a contact. */
    fun addContact() {
        repeat(15) {
            if (textNode("Remove Contact") != null) {
                return
            }
            if (textNode("Add Contact") != null) {
                tapText("Add Contact")
                awaitValue("Remove contact after saving") { textNode("Remove Contact") }
                check(textNode("Add Contact") == null) { "Add contact is still visible after saving." }
                return
            }
            val scroll = nodes(instrumentation.uiAutomation.rootInActiveWindow)
                .filter { it.isScrollable && it.isVisibleToUser }
                .maxByOrNull { nodeBounds(it).let { bounds -> bounds.width().toLong() * bounds.height() } }
            check(scroll != null) { "Contact actions are not visible and details cannot scroll." }
            scroll.performAction(AccessibilityNodeInfo.ACTION_SCROLL_FORWARD)
            Thread.sleep(250)
        }
        error("Add contact did not become visible in identity details.")
    }

    fun dismissPetitionSentDialogIfNeeded(): Boolean {
        val petitionDialogIsVisible = awaitValue("petition confirmation or identity details") {
            when {
                textNode("Petition sent") != null -> true
                identityDetailsAreVisible() -> false
                else -> null
            }
        }
        if (petitionDialogIsVisible) {
            tapText("OK")
        }
        return petitionDialogIsVisible
    }

    private fun identityDetailsAreVisible(): Boolean {
        var isVisible = false
        instrumentation.runOnMainSync {
            isVisible = ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
                .asSequence()
                .flatMap { views(it.window.decorView) }
                .any { AutomationIdMatcher.matches(it, ViewIdentityScreen.SCREEN) && it.isShown }
        }
        return isVisible
    }

    private fun textNode(text: String): AccessibilityNodeInfo? =
        nodes(instrumentation.uiAutomation.rootInActiveWindow)
            .firstOrNull { it.isVisibleToUser && it.text?.toString() == text && !nodeBounds(it).isEmpty }

    private fun nodes(node: AccessibilityNodeInfo?): Sequence<AccessibilityNodeInfo> = sequence {
        if (node != null) {
            yield(node)
            for (index in 0 until node.childCount) yieldAll(nodes(node.getChild(index)))
        }
    }

    private fun views(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) for (index in 0 until view.childCount) yieldAll(views(view.getChildAt(index)))
    }

    private fun nodeBounds(node: AccessibilityNodeInfo): Rect = Rect().also { node.getBoundsInScreen(it) }

    private fun tap(bounds: Rect) {
        val downTime = SystemClock.uptimeMillis()
        for (action in listOf(MotionEvent.ACTION_DOWN, MotionEvent.ACTION_UP)) {
            val event = MotionEvent.obtain(downTime, SystemClock.uptimeMillis(), action,
                bounds.exactCenterX(), bounds.exactCenterY(), 0)
            event.source = InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(instrumentation.uiAutomation.injectInputEvent(event, true)) { "UI tap failed." }
            } finally {
                event.recycle()
            }
            if (action == MotionEvent.ACTION_DOWN) Thread.sleep(80)
        }
    }

    private fun <T : Any> awaitValue(description: String, read: () -> T?): T {
        val deadline = SystemClock.elapsedRealtime() + 20_000
        do {
            read()?.let { return it }
            Thread.sleep(100)
        } while (SystemClock.elapsedRealtime() < deadline)
        error("Timed out waiting for $description.")
    }
}
