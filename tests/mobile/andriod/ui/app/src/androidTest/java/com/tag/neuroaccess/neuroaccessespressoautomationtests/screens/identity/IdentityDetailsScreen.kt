package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity

import android.graphics.Rect
import android.os.SystemClock
import android.view.InputDevice
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScrollableScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract

/** Uses stable schema identity to read the real card, including while its background animates. */
object IdentityDetailsScreen {
    private val instrumentation = InstrumentationRegistry.getInstrumentation()
    // This is the production schema key, not the person's identity value or translated label.
    private val neuroIdLabel = UiIds.forKey(UiIds.LabelIdentityCardFieldname, "IDENTITY_ID")
    private val neuroIdValue = UiIds.forKey(UiIds.LabelIdentityCardFieldvalue, "IDENTITY_ID")

    /** Expands the existing details panel and reads the uniquely identified visible Neuro-ID. */
    fun openAndReadNeuroId(): String {
        ViewIdentityScreen.assertDisplayed()
        this.ensureExpanded()
        for (id in listOf(neuroIdLabel, neuroIdValue)) {
            ScrollableScreen.waitForVisibleMatch(ViewIdentityScreen.SCREEN,
                UiIds.ControlIdentityCardScroll, "the identified Neuro-ID field") {
                AutomationIdMatcher.matches(it, id)
            }
        }
        UiContract.assertLabel(neuroIdLabel, UiText.text("NeuroID"), ViewIdentityScreen.SCREEN)
        val identityId = UiContract.readVisibleLabel(neuroIdValue, ViewIdentityScreen.SCREEN)
        check(Regex("[^\\s@]+@[^\\s@]+").matches(identityId)) {
            "UI: the identified Neuro-ID field has an invalid value (redacted)."
        }
        return identityId
    }

    private fun ensureExpanded() {
        if (ScreenWaiter.isDisplayedOnScreen(UiIds.ControlIdentityCardScroll, ViewIdentityScreen.SCREEN)) return
        UiContract.assertLabel(UiIds.LabelIdentityCardDetails, UiText.text("SwipeForDetails"), ViewIdentityScreen.SCREEN)
        var original: Rect? = null
        this.instrumentation.runOnMainSync { original = this.headerBounds() }
        val collapsedHeader = checkNotNull(original) { "TECHNICAL: missing identified details header." }
        ScreenWaiter.clickToLiveScreen(UiIds.LabelIdentityCardDetails, ViewIdentityScreen.SCREEN)
        if (!this.waitForExpandedHeader(collapsedHeader)) {
            var bounds: Rect? = null
            this.instrumentation.runOnMainSync { bounds = this.screen()?.let(this::visibleBounds) }
            val screenBounds = checkNotNull(bounds) { "TECHNICAL: missing identity screen bounds." }
            this.swipeUp(collapsedHeader.exactCenterX(), collapsedHeader.exactCenterY(),
                screenBounds.top + screenBounds.height() * 0.2f)
            check(this.waitForExpandedHeader(collapsedHeader)) { "UI: identity details did not expand." }
        }
    }

    private fun waitForExpandedHeader(original: Rect): Boolean {
        val deadline = SystemClock.elapsedRealtime() + 2_000L
        do {
            var expanded = false
            this.instrumentation.runOnMainSync {
                val header = this.headerBounds()
                expanded = header != null && header.top < original.top - original.height()
            }
            if (expanded && ScreenWaiter.isDisplayedOnScreen(
                    UiIds.ControlIdentityCardScroll, ViewIdentityScreen.SCREEN)) return true
            Thread.sleep(100)
        } while (SystemClock.elapsedRealtime() < deadline)
        return false
    }

    private fun headerBounds(): Rect? = this.screen()?.let { screen ->
        val headers = this.descendants(screen).filter {
            AutomationIdMatcher.matches(it, UiIds.LabelIdentityCardDetails) && this.visibleBounds(it) != null
        }.toList()
        check(headers.size <= 1) { "TECHNICAL: duplicate identity details header." }
        headers.singleOrNull()?.let(this::visibleBounds)
    }

    private fun screen(): View? {
        val screens = ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
            .asSequence().flatMap { this.descendants(it.window.decorView) }
            .filter { AutomationIdMatcher.matches(it, ViewIdentityScreen.SCREEN) && this.visibleBounds(it) != null }
            .toList()
        check(screens.size <= 1) { "TECHNICAL: duplicate active identity screens." }
        return screens.singleOrNull()
    }

    private fun descendants(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) for (index in 0 until view.childCount) yieldAll(descendants(view.getChildAt(index)))
    }

    private fun visibleBounds(view: View): Rect? {
        val bounds = Rect()
        return bounds.takeIf { view.isShown && view.getGlobalVisibleRect(it) && !it.isEmpty }
    }

    private fun swipeUp(x: Float, startY: Float, endY: Float) {
        val downTime = SystemClock.uptimeMillis()
        fun inject(action: Int, y: Float) {
            val event = MotionEvent.obtain(downTime, SystemClock.uptimeMillis(), action, x, y, 0)
            event.source = InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(this.instrumentation.uiAutomation.injectInputEvent(event, true)) {
                    "TECHNICAL: could not interact with identity details."
                }
            } finally { event.recycle() }
        }
        inject(MotionEvent.ACTION_DOWN, startY)
        try {
            for (step in 1..20) {
                Thread.sleep(25)
                inject(MotionEvent.ACTION_MOVE, startY + (endY - startY) * step / 20)
            }
            Thread.sleep(100)
            inject(MotionEvent.ACTION_MOVE, endY)
            inject(MotionEvent.ACTION_UP, endY)
        } catch (failure: Exception) {
            inject(MotionEvent.ACTION_CANCEL, endY)
            throw failure
        }
    }
}
