package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.graphics.Rect
import android.os.Bundle
import android.os.SystemClock
import android.view.InputDevice
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher

/** Opens and reads identity details while the ID card continues animating. */
object IdentityDetailsScreen {
    private const val TIMEOUT_MILLISECONDS = 15_000L
    private val instrumentation = InstrumentationRegistry.getInstrumentation()
    private val identityPattern = Regex("[^\\s@]+@[^\\s@]+")

    /**
     * Opens Swipe for details, scrolls to Neuro-ID and reads the displayed value.
     * @return The identity ID displayed in the English detail panel.
     */
    fun openAndReadNeuroId(): String {
        ViewIdentityScreen.assertDisplayed()
        val collapsedHeader = this.awaitValue("Swipe for details") { this.headerBounds() }
        this.gesture(collapsedHeader.exactCenterX(), collapsedHeader.exactCenterY(),
            collapsedHeader.exactCenterY(), 100)
        if (!this.waitForExpandedHeader(collapsedHeader)) {
            val screenBounds = this.awaitValue("the ID screen bounds") { this.screen()?.let(this::visibleBounds) }
            this.gesture(collapsedHeader.exactCenterX(), collapsedHeader.exactCenterY(),
                screenBounds.top + screenBounds.height() * 0.2f, 500)
            check(this.waitForExpandedHeader(collapsedHeader)) { "Swipe for details did not expand." }
        }
        this.announce("Quick Login: identity details opened.")

        repeat(12) {
            var identityId: String? = null
            var scrollBounds: Rect? = null
            this.instrumentation.runOnMainSync {
                val screen = this.screen()
                if (screen != null) {
                    val texts = this.descendants(screen).filterIsInstance<TextView>().toList()
                    val label = texts.firstOrNull { it.text.toString().trim() == "Neuro-ID" }
                    val value = label?.let(this::fieldValue)
                    if (label != null && value != null && this.fullyVisible(label) && this.fullyVisible(value)) {
                        identityId = value.text.toString().trim()
                    } else {
                        // MAUI may use ScrollView or NestedScrollView on Android.
                        scrollBounds = this.descendants(screen)
                            .filter { it.canScrollVertically(1) }
                            .mapNotNull(this::visibleBounds)
                            .filter { it.height() > 100 }
                            .maxByOrNull { it.width().toLong() * it.height() }
                    }
                }
            }
            identityId?.let {
                this.announce("Quick Login: Neuro-ID read from the visible detail field.")
                return it
            }
            val bounds = scrollBounds
            if (bounds != null) {
                this.gesture(bounds.exactCenterX(), bounds.top + bounds.height() * 0.8f,
                    bounds.top + bounds.height() * 0.3f, 400)
            }
            Thread.sleep(250)
        }
        error("Neuro-ID was not visible after scrolling through the identity details.")
    }

    private fun fieldValue(label: TextView): TextView? {
        var parent = label.parent as? ViewGroup
        repeat(4) {
            val container = parent ?: return null
            val values = this.descendants(container).filterIsInstance<TextView>()
                .filter { it !== label && it.text.toString().isNotBlank() }.toList()
            if (values.isNotEmpty()) {
                // Stop at the field container; never read a neighbouring JID or attachment ID.
                return values.singleOrNull()?.takeIf { this.identityPattern.matches(it.text.toString().trim()) }
            }
            parent = container.parent as? ViewGroup
        }
        return null
    }

    private fun waitForExpandedHeader(original: Rect): Boolean {
        val deadline = SystemClock.elapsedRealtime() + 2_000L
        do {
            var expanded = false
            this.instrumentation.runOnMainSync {
                val header = this.headerBounds()
                expanded = header != null && header.top < original.top - original.height()
            }
            if (expanded) {
                Thread.sleep(250)
                return true
            }
            Thread.sleep(100)
        } while (SystemClock.elapsedRealtime() < deadline)
        return false
    }

    private fun headerBounds(): Rect? = this.screen()?.let { screen ->
        this.descendants(screen).filterIsInstance<TextView>()
            .firstOrNull { it.text.toString() == "Swipe for details" && this.visibleBounds(it) != null }
            ?.let(this::visibleBounds)
    }

    private fun screen(): View? =
        ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
            .asSequence().flatMap { this.descendants(it.window.decorView) }
            .firstOrNull { AutomationIdMatcher.matches(it, ViewIdentityScreen.SCREEN) && it.isShown }

    private fun descendants(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) {
            for (index in 0 until view.childCount) yieldAll(descendants(view.getChildAt(index)))
        }
    }

    private fun visibleBounds(view: View): Rect? {
        val bounds = Rect()
        return bounds.takeIf { view.isShown && view.getGlobalVisibleRect(it) && !it.isEmpty }
    }

    private fun fullyVisible(view: View): Boolean =
        this.visibleBounds(view)?.let { it.height() >= view.height && it.width() >= view.width } == true

    private fun <T : Any> awaitValue(description: String, read: () -> T?): T {
        val deadline = SystemClock.elapsedRealtime() + TIMEOUT_MILLISECONDS
        do {
            var result: T? = null
            this.instrumentation.runOnMainSync { result = read() }
            result?.let { return it }
            Thread.sleep(100)
        } while (SystemClock.elapsedRealtime() < deadline)
        error("Timed out waiting for $description on Show ID.")
    }

    private fun gesture(x: Float, startY: Float, endY: Float, duration: Long) {
        val downTime = SystemClock.uptimeMillis()
        fun inject(action: Int, y: Float) {
            val event = MotionEvent.obtain(downTime, SystemClock.uptimeMillis(), action, x, y, 0)
            event.source = InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(this.instrumentation.uiAutomation.injectInputEvent(event, true)) {
                    "Could not interact with identity details."
                }
            } finally {
                event.recycle()
            }
        }
        inject(MotionEvent.ACTION_DOWN, startY)
        try {
            val steps = 20
            for (index in 1..steps) {
                Thread.sleep(duration / steps)
                if (startY != endY) inject(MotionEvent.ACTION_MOVE, startY + (endY - startY) * index / steps)
            }
            if (startY != endY) {
                // Release at rest so a fling cannot skip the Neuro-ID field between reads.
                Thread.sleep(100)
                inject(MotionEvent.ACTION_MOVE, endY)
            }
            inject(MotionEvent.ACTION_UP, endY)
        } catch (failure: Exception) {
            inject(MotionEvent.ACTION_CANCEL, endY)
            throw failure
        }
    }

    private fun announce(message: String) {
        this.instrumentation.sendStatus(2, Bundle().apply { putString("stream", "\n$message\n") })
    }
}


