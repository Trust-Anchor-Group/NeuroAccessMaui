package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object ApplicationsScreen {
    const val SCREEN = "screen_identity_applications"
    private const val CURRENT_APPLICATION = "button_current_identity_application"
    private const val AVAILABLE_APPLICATION = "button_available_identity_application"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(SCREEN))
            .check(matches(isDisplayed()))
    }

    fun openPersonalIdApplication() {
        val applicationAutomationId = ScreenWaiter.waitForAny(
            CURRENT_APPLICATION,
            AVAILABLE_APPLICATION
        )

        ScreenWaiter.performActionAndWaitFor(KycProcessScreen.SCREEN) {
            onView(withAutomationId(applicationAutomationId))
                .perform(click())
        }
    }
}
