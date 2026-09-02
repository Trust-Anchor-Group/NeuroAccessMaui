package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter

object PhoneVerificationScreen {

    const val SCREEN = "screen_phone_verification"
    private const val COUNTRY_SELECTOR = "button_select_phone_country"
    private const val COUNTRY_SELECTOR_POPUP = "popup_select_phone_country"
    private const val COUNTRY_SEARCH = "input_search_phone_country"
    private const val UNITED_STATES = "option_phone_country_US"
    private const val PHONE_NUMBER = "input_phone_number"
    private const val SEND_CODE = "button_send_phone_code"
    private const val BACK = "button_back_onboarding"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(COUNTRY_SELECTOR))
            .check(matches(isDisplayed()))
        onView(withContentDescription(PHONE_NUMBER))
            .check(matches(isDisplayed()))
        onView(withContentDescription(SEND_CODE))
            .check(matches(isDisplayed()))
    }

    fun selectUnitedStates() {
        ScreenWaiter.performActionAndWaitFor(COUNTRY_SELECTOR_POPUP) {
            onView(withContentDescription(COUNTRY_SELECTOR))
                .perform(click())
        }
        onView(withContentDescription(COUNTRY_SEARCH))
            .perform(replaceText("USA"), closeSoftKeyboard())
        ScreenWaiter.waitFor(UNITED_STATES)
        onView(withContentDescription(UNITED_STATES))
            .perform(click())
        ScreenWaiter.waitUntilHidden(COUNTRY_SELECTOR_POPUP)
    }

    fun enterPhoneNumber(phoneNumber: String) {
        onView(withContentDescription(PHONE_NUMBER))
            .perform(replaceText(phoneNumber), closeSoftKeyboard())
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

