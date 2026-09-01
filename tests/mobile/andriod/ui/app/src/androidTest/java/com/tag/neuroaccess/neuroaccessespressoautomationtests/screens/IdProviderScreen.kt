package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object IdProviderScreen {

    const val SCREEN = "screen_onboarding_id_provider"
    private const val SELECT_FOR_ME = "button_id_provider_select_for_me"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(SELECT_FOR_ME))
            .check(matches(isDisplayed()))
    }

    fun selectForMe() {
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withContentDescription(SELECT_FOR_ME))
                .perform(click())
        }
    }
}