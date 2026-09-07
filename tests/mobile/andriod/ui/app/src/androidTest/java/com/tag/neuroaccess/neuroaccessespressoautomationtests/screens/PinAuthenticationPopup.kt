package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

object PinAuthenticationPopup {
    const val POPUP = "popup_pin_authentication"
    private const val PIN = "input_authentication_pin"
    private const val CONFIRM = "button_authentication_pin_confirm"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(POPUP)
        onView(withContentDescription(POPUP)).check(matches(isDisplayed()))
    }

    fun enterPinAndWaitFor(pin: String, nextScreen: String) {
        onView(
            allOf(
                isAssignableFrom(EditText::class.java),
                isDescendantOfA(withContentDescription(PIN))
            )
        ).perform(click(), replaceText(pin), closeSoftKeyboard())

        ScreenWaiter.performActionAndWaitFor(nextScreen) {
            onView(withContentDescription(CONFIRM)).perform(click())
        }
    }
}
