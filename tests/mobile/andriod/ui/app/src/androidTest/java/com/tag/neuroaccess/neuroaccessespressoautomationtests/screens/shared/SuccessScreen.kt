package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText

object SuccessScreen {
    const val SCREEN = UiIds.ScreenOnboardingFinalize
    private const val CONTINUE = UiIds.ButtonOnboardingFinalizeContinue

    fun continueToHome(waitForHome: Boolean = true) {
        ScreenWaiter.waitFor(SCREEN)
        ScreenWaiter.waitUntilReady(CONTINUE)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingFinalizeTitle, UiText.text("OnboardingFinalizePageTitle"), SCREEN)
        UiContract.assertAction(CONTINUE, UiText.text("Continue"), enabled = true, screenId = SCREEN)
        onView(withAutomationId(CONTINUE, SCREEN))
            .check(matches(isDisplayed()))
            .perform(click())
        if (waitForHome) {
            ScreenWaiter.waitFor(HomeScreen.SCREEN)
        }
    }
}
