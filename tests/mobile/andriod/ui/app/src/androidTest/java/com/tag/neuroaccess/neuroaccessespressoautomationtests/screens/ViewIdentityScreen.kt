package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object ViewIdentityScreen {
    const val SCREEN = "screen_view_identity"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(AutomationIdMatcher.withAutomationId(SCREEN)).check(matches(isDisplayed()))
    }
}
