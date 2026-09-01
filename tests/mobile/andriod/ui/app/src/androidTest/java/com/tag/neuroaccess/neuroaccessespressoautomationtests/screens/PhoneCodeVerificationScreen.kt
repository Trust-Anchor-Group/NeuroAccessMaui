package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object PhoneCodeVerificationScreen {

    const val SCREEN = "screen_phone_code_verification"
    private const val BACK = "button_back_phone_verification"
    private const val VERIFY_CODE = "button_verify_phone_code"
    private const val RESEND_CODE = "button_resend_phone_code"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(VERIFY_CODE))
            .check(matches(isDisplayed()))
        onView(withContentDescription(RESEND_CODE))
            .check(matches(isDisplayed()))
        onView(withContentDescription(BACK))
            .check(matches(isDisplayed()))
    }

    fun goBack() {
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withContentDescription(BACK))
                .perform(click())
        }
    }
}