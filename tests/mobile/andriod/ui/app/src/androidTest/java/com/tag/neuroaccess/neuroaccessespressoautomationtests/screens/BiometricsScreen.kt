package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object BiometricsScreen {
    const val SCREEN = "screen_biometrics"
    private const val LATER = "button_biometrics_later"

    fun skip() {
        ScreenWaiter.performActionAndWaitFor(SuccessScreen.SCREEN) {
            onView(withAutomationId(LATER))
                .check(matches(isDisplayed()))
                .perform(click())
        }
    }
}