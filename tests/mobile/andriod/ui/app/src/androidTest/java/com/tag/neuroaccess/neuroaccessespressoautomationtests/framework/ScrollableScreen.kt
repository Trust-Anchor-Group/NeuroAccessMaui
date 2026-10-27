package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.graphics.Rect
import android.os.SystemClock
import android.view.InputDevice
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import androidx.test.espresso.matcher.ViewMatchers.isDisplayingAtLeast
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage

/** Reads visible native views and scrolls with touch gestures while MAUI continues updating. */
object ScrollableScreen {
    /**
     * Scrolls forward until a matching view is visible inside the requested screen.
     * @param screenId Automation ID limiting the search to one screen.
     * @param scrollId Automation ID limiting gestures to its scrolling content.
     * @param description Failure diagnostic identifying the expected UI state.
     * @param timeoutMillis Maximum wait, including loading and scrolling.
     * @param matches Predicate evaluated only on the Android main thread.
     */
    fun waitForVisibleMatch(screenId: String, scrollId: String, description: String,
                            timeoutMillis: Long = 30_000, matches: (View) -> Boolean) {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.elapsedRealtime() + timeoutMillis
        do {
            var found = false
            var scrollBounds: Rect? = null
            instrumentation.runOnMainSync {
                val screens = ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED)
                    .asSequence().flatMap { descendants(it.window.decorView) }
                    .filter { it.isShown && isDisplayingAtLeast(1).matches(it) && AutomationIdMatcher.matches(it, screenId) }
                    .toList()
                check(screens.size <= 1) { "TECHNICAL: duplicate active scroll screen." }
                val screen = screens.singleOrNull()
                if (screen != null) {
                    val targets = descendants(screen).filter { it.isShown && matches(it) }.toList()
                    check(targets.size <= 1) { "TECHNICAL: duplicate identified scroll target." }
                    found = targets.singleOrNull()?.let { isDisplayingAtLeast(90).matches(it) } == true
                    if (!found) {
                        val containers = descendants(screen).filter { it.isShown && AutomationIdMatcher.matches(it, scrollId) }.toList()
                        check(containers.size <= 1) { "TECHNICAL: duplicate identified scroll container." }
                        val bounds = containers.singleOrNull()?.let { descendants(it) }
                            ?.filter { it.isShown && it.canScrollVertically(1) }
                            ?.mapNotNull { view -> Rect().takeIf { view.getGlobalVisibleRect(it) && it.height() > 100 } }
                            ?.toList().orEmpty()
                        check(bounds.size <= 1) { "TECHNICAL: ambiguous native scroll surface within the identified container." }
                        scrollBounds = bounds.singleOrNull()
                    }
                }
            }
            if (found) return
            scrollBounds?.let(::swipeForward)
            Thread.sleep(250)
        } while (SystemClock.elapsedRealtime() < deadline)
        error("Timed out waiting for $description on $screenId.")
    }

    private fun descendants(view: View): Sequence<View> = sequence {
        yield(view)
        if (view is ViewGroup) for (index in 0 until view.childCount) yieldAll(descendants(view.getChildAt(index)))
    }

    private fun swipeForward(bounds: Rect) {
        val automation = InstrumentationRegistry.getInstrumentation().uiAutomation
        val downTime = SystemClock.uptimeMillis()
        val startY = bounds.top + bounds.height() * 0.8f
        val endY = bounds.top + bounds.height() * 0.3f
        fun inject(action: Int, y: Float) {
            val event = MotionEvent.obtain(downTime, SystemClock.uptimeMillis(), action, bounds.exactCenterX(), y, 0)
            event.source = InputDevice.SOURCE_TOUCHSCREEN
            try {
                check(automation.injectInputEvent(event, true)) { "Could not scroll the screen." }
            } finally {
                event.recycle()
            }
        }
        inject(MotionEvent.ACTION_DOWN, startY)
        try {
            for (step in 1..16) {
                Thread.sleep(15)
                inject(MotionEvent.ACTION_MOVE, startY + (endY - startY) * step / 16)
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
