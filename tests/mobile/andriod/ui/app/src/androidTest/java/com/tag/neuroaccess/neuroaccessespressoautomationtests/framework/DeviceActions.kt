package com.tag.neuroaccess.neuroaccessespressoautomationtests.framework

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.GeneralSwipeAction
import androidx.test.espresso.action.Press
import androidx.test.espresso.action.Swipe
import androidx.test.espresso.matcher.ViewMatchers.isRoot

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

    private const val SWIPE_START_HEIGHT_RATIO = 0.75f
    private const val SWIPE_END_HEIGHT_RATIO = 0.25f
}
