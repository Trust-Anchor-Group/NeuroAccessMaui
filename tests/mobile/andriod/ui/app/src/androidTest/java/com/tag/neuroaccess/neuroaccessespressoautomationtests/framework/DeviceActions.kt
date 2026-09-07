package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import android.graphics.Rect
import android.os.SystemClock
import android.view.MotionEvent
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.GeneralSwipeAction
import androidx.test.espresso.action.Press
import androidx.test.espresso.action.Swipe
import androidx.test.espresso.matcher.ViewMatchers.isRoot
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage

object DeviceActions {

    fun swipeUpInApplication() {
        onView(isRoot()).perform(
            GeneralSwipeAction(
                Swipe.SLOW,
                { view ->
                    floatArrayOf(
                        view.width / 2f,
                        view.height * SWIPE_START_HEIGHT_RATIO
                    )
                },
                { view ->
                    floatArrayOf(
                        view.width / 2f,
                        view.height * SWIPE_END_HEIGHT_RATIO
                    )
                },
                Press.FINGER
            )
        )
    }

    fun tapFromApplicationBottom(bottomOffsetPixels: Int, delayMilliseconds: Long) {
        require(bottomOffsetPixels >= 0) { "Bottom offset must not be negative." }
        require(delayMilliseconds >= 0L) { "Delay must not be negative." }

        SystemClock.sleep(delayMilliseconds)

        val instrumentation = InstrumentationRegistry.getInstrumentation()
        var tapX = 0f
        var tapY = 0f

        instrumentation.runOnMainSync {
            val resumedActivity = checkNotNull(
                ActivityLifecycleMonitorRegistry.getInstance()
                    .getActivitiesInStage(Stage.RESUMED)
                    .firstOrNull()
            ) { "No resumed activity was available for the device tap." }
            val rootView = resumedActivity.window.decorView
            val visibleApplicationBounds = Rect()
            rootView.getWindowVisibleDisplayFrame(visibleApplicationBounds)
            check(!visibleApplicationBounds.isEmpty) {
                "The visible application bounds were unavailable for the device tap."
            }

            tapX = visibleApplicationBounds.exactCenterX()
            tapY = visibleApplicationBounds.bottom - bottomOffsetPixels.toFloat()
        }

        val eventTime = SystemClock.uptimeMillis()
        val downEvent = MotionEvent.obtain(
            eventTime,
            eventTime,
            MotionEvent.ACTION_DOWN,
            tapX,
            tapY,
            0
        )
        val upEvent = MotionEvent.obtain(
            eventTime,
            eventTime + 50L,
            MotionEvent.ACTION_UP,
            tapX,
            tapY,
            0
        )

        try {
            instrumentation.sendPointerSync(downEvent)
            instrumentation.sendPointerSync(upEvent)
        } finally {
            downEvent.recycle()
            upEvent.recycle()
        }
    }

    private const val SWIPE_START_HEIGHT_RATIO = 0.75f
    private const val SWIPE_END_HEIGHT_RATIO = 0.25f
}
