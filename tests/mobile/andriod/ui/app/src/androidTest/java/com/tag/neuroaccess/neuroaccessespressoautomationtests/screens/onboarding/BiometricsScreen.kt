package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.SuccessScreen

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText

object BiometricsScreen {
    const val SCREEN = UiIds.ScreenOnboardingBiometrics
    private const val LATER = UiIds.ButtonOnboardingBiometricsLater

    fun skip() {
        ScreenWaiter.waitUntilReady(LATER)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingBiometricsTitle, UiText.text("OnboardingBiometricsPageTitle"), SCREEN)
        UiContract.assertAction(LATER, UiText.text("Later"), enabled = true, screenId = SCREEN)
        ScreenWaiter.performActionAndWaitFor(SuccessScreen.SCREEN) {
            onView(withAutomationId(LATER, SCREEN))
                .check(matches(isDisplayed()))
                .perform(click())
        }
    }
}
