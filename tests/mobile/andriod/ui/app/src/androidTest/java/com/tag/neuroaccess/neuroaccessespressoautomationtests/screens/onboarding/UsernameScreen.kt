package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationIdOrAncestor
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import org.hamcrest.Matchers.allOf

object UsernameScreen {
    const val SCREEN = UiIds.ScreenOnboardingUsername
    private const val USERNAME = UiIds.InputOnboardingUsernameValue
    private const val CONTINUE = UiIds.ButtonOnboardingUsernameContinue

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        ScreenWaiter.waitFor(USERNAME)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingUsernameTitle, UiText.text("OnboardingNameEntryTitle"), SCREEN)
        UiContract.assertInput(USERNAME, enabled = true, screenId = SCREEN)
        UiContract.assertAction(CONTINUE, UiText.text("Continue"), enabled = false, screenId = SCREEN)
    }

    fun enterUsername(username: String) {
        UiContract.enterText(USERNAME, username, SCREEN)
    }

    fun continueToPinCreation() {
        ScreenWaiter.waitUntilReady(CONTINUE)
        UiContract.assertAction(CONTINUE, UiText.text("Continue"), enabled = true, screenId = SCREEN)
        onView(withAutomationId(CONTINUE, SCREEN)).perform(click())
        when (ScreenWaiter.waitForAny(UiIds.ScreenOnboardingAccount, PinCreationScreen.SCREEN)) {
            UiIds.ScreenOnboardingAccount -> {
                UiContract.assertScreen(UiIds.ScreenOnboardingAccount)
                UiContract.assertLabel(UiIds.LabelOnboardingAccountTitle,
                    UiText.text("OnboardingAccountPageTitle"), UiIds.ScreenOnboardingAccount)
                ScreenWaiter.waitFor(PinCreationScreen.SCREEN)
            }
            PinCreationScreen.SCREEN -> Unit
        }
        ScreenWaiter.waitUntilHidden(SCREEN)
    }
}
