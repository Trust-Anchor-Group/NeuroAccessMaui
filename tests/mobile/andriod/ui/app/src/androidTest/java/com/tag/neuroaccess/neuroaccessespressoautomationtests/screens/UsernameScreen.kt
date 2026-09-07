package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

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
import org.hamcrest.Matchers.allOf

object UsernameScreen {
    const val SCREEN = "screen_username"
    private const val USERNAME = "input_username"
    private const val CONTINUE = "button_continue_username"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(USERNAME))
            .check(matches(isDisplayed()))
        onView(withAutomationId(CONTINUE))
            .check(matches(isDisplayed()))
    }

    fun enterUsername(username: String) {
        onView(
            allOf(
                isAssignableFrom(EditText::class.java),
                withAutomationIdOrAncestor(USERNAME)
            )
        ).perform(click(), replaceText(username), closeSoftKeyboard())
    }

    fun continueToPinCreation() {
        ScreenWaiter.performActionAndWaitFor(PinCreationScreen.SCREEN) {
            onView(withAutomationId(CONTINUE))
                .perform(click())
        }
    }
}
