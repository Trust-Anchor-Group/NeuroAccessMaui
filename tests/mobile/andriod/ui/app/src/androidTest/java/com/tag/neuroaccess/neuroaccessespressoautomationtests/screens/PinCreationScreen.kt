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

object PinCreationScreen {
    const val SCREEN = "screen_create_pin"
    private const val NEW_PIN = "input_new_pin"
    private const val CONFIRM_PIN = "input_confirm_pin"
    private const val CREATE_PIN = "button_create_pin"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(NEW_PIN))
            .check(matches(isDisplayed()))
        onView(withAutomationId(CONFIRM_PIN))
            .check(matches(isDisplayed()))
        onView(withAutomationId(CREATE_PIN))
            .check(matches(isDisplayed()))
    }

    fun enterPin(pin: String) {
        this.enterTextInto(NEW_PIN, pin)
        this.enterTextInto(CONFIRM_PIN, pin)
    }

    fun createPinAndWaitForNextStep(): String {
        onView(withAutomationId(CREATE_PIN))
            .perform(click())

        return ScreenWaiter.waitForAny(BiometricsScreen.SCREEN, SuccessScreen.SCREEN)
    }


    private fun enterTextInto(automationId: String, text: String) {
        onView(
            allOf(
                isAssignableFrom(EditText::class.java),
                withAutomationIdOrAncestor(automationId)
            )
        ).perform(click(), replaceText(text), closeSoftKeyboard())
    }
}
