package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.SuccessScreen

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText

object PhoneVerificationScreen {

    const val SCREEN = UiIds.ScreenOnboardingPhone
    private const val COUNTRY_SELECTOR = UiIds.ButtonOnboardingPhoneCountry
    private const val COUNTRY_SELECTOR_POPUP = UiIds.PopupSharedPhonecountry
    private const val COUNTRY_SEARCH = UiIds.InputSharedPhonecountrySearch
    private val UNITED_STATES = UiIds.forKey(UiIds.ItemSharedPhonecountryOption, "US")
    private const val PHONE_NUMBER = UiIds.InputOnboardingPhoneNumber
    private const val SEND_CODE = UiIds.ButtonOnboardingPhoneSendCode
    private const val BACK = UiIds.ButtonOnboardingRootBack

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingPhoneTitle, UiText.text("OnboardingPhonePageTitle"), SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingPhoneNumber, UiText.text("PhoneSectionTitle"), SCREEN)
        // The initial country comes from the device; USA is asserted after explicit selection.
        UiContract.assertInput(PHONE_NUMBER, enabled = true, screenId = SCREEN)
        UiContract.assertAction(SEND_CODE, UiText.text("SendCode"), enabled = false, screenId = SCREEN)
    }

    fun selectUnitedStates() {
        ScreenWaiter.performActionAndWaitFor(COUNTRY_SELECTOR_POPUP) {
            onView(withAutomationId(COUNTRY_SELECTOR, SCREEN))
                .perform(click())
        }
        UiContract.enterText(COUNTRY_SEARCH, "USA", COUNTRY_SELECTOR_POPUP)
        ScreenWaiter.waitFor(UNITED_STATES)
        onView(withAutomationId(UNITED_STATES, COUNTRY_SELECTOR_POPUP))
            .perform(click())
        ScreenWaiter.waitUntilHidden(COUNTRY_SELECTOR_POPUP)
        UiContract.assertAction(COUNTRY_SELECTOR, "1", enabled = true, screenId = SCREEN)
    }

    fun enterPhoneNumber(phoneNumber: String) {
        UiContract.enterText(PHONE_NUMBER, phoneNumber, SCREEN)
    }

    fun sendCodeAfterRevocation(): String {
        when (ScreenWaiter.waitForAnyReady(UiIds.ButtonOnboardingFinalizeContinue, SEND_CODE)) {
            UiIds.ButtonOnboardingFinalizeContinue -> return SuccessScreen.SCREEN
        }
        onView(withAutomationId(SEND_CODE, SCREEN)).perform(click())
        return ScreenWaiter.waitForAny(SuccessScreen.SCREEN, PhoneCodeVerificationScreen.SCREEN)
    }
    fun sendCode() {
        UiContract.assertAction(SEND_CODE, UiText.text("SendCode"), enabled = true, screenId = SCREEN)
        ScreenWaiter.performActionAndWaitFor(PhoneCodeVerificationScreen.SCREEN) {
            onView(withAutomationId(SEND_CODE, SCREEN))
                .perform(click())
        }
    }

    fun goBack() {
        ScreenWaiter.performActionAndWaitFor(IdProviderScreen.SCREEN) {
            onView(withAutomationId(BACK, UiIds.ScreenOnboardingRoot))
                .perform(click())
        }
    }
}
