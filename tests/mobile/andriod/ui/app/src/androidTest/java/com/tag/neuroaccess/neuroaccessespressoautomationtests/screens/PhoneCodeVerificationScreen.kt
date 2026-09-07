package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object PhoneCodeVerificationScreen {

    const val SCREEN = "screen_phone_code_verification"
    private const val VERIFICATION_CODE = "input_phone_verification_code"
    private const val BACK = "button_back_phone_verification"
    private const val VERIFY_CODE = "button_verify_phone_code"
    private const val RESEND_CODE = "button_resend_phone_code"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withAutomationId(VERIFY_CODE))
            .check(matches(isDisplayed()))
        onView(withAutomationId(RESEND_CODE))
            .check(matches(isDisplayed()))
        onView(withAutomationId(BACK))
            .check(matches(isDisplayed()))
    }

    fun goBack() {
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withAutomationId(BACK))
                .perform(click())
        }
    }

    fun enterVerificationCode(verificationCode: String) {
        onView(withAutomationId(VERIFICATION_CODE))
            .perform(replaceText(verificationCode), closeSoftKeyboard())
    }

    fun verifyCodeAndWaitFor(nextScreen: String) {
        ScreenWaiter.performActionAndWaitFor(nextScreen) {
            onView(withAutomationId(VERIFY_CODE))
                .perform(click())
        }
    }
}
