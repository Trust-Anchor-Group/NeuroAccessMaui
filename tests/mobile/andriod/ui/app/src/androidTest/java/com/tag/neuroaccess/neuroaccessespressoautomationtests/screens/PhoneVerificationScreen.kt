package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object PhoneVerificationScreen {

    const val SCREEN = "screen_phone_verification"
    private const val PHONE_NUMBER = "input_phone_number"
    private const val SEND_CODE = "button_send_phone_code"
    private const val BACK = "button_back_onboarding"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(PHONE_NUMBER))
            .check(matches(isDisplayed()))
        onView(withContentDescription(SEND_CODE))
            .check(matches(isDisplayed()))
    }

    fun enterPhoneNumber(phoneNumber: String) {
        onView(withContentDescription(PHONE_NUMBER))
            .perform(replaceText(phoneNumber))
    }

    fun sendCode() {
        ScreenWaiter.performActionAndWaitFor(PhoneCodeVerificationScreen.SCREEN) {
            onView(withContentDescription(SEND_CODE))
                .perform(click())
        }
    }

    fun goBack() {
        ScreenWaiter.performActionAndWaitFor(IdProviderScreen.SCREEN) {
            onView(withContentDescription(BACK))
                .perform(click())
        }
    }
}