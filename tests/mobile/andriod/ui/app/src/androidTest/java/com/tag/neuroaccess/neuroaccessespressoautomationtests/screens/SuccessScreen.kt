package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object SuccessScreen {
    const val SCREEN = "screen_success"
    private const val CONTINUE = "button_continue_success"

    fun continueToHome() {
        ScreenWaiter.performActionAndWaitFor(HomeScreen.SCREEN) {
            onView(withContentDescription(CONTINUE))
                .check(matches(isDisplayed()))
                .perform(click())
        }
    }
}