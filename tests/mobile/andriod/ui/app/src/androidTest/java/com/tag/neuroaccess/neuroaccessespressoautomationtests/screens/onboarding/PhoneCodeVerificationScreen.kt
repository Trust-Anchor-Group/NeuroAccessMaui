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
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.VerificationCodeActions
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText

object PhoneCodeVerificationScreen {

    const val SCREEN = UiIds.ScreenSecurityVerifycode
    private const val BACK = UiIds.ButtonSecurityVerifycodeBack
    private const val VERIFY_CODE = UiIds.ButtonSecurityVerifycodeVerify
    private const val RESEND_CODE = UiIds.ButtonSecurityVerifycodeResend

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelSecurityVerifycodeTitle, UiText.text("OnboardingVerifyCodePageTitle"), SCREEN)
        UiContract.assertLabel(UiIds.LabelSecurityVerifycodeDigits, UiText.text("VerifyCodeSectionTitle"), SCREEN)
        UiContract.assertCodeDigits(UiIds.ControlSecurityVerifycodeDigits, SCREEN)
        UiContract.assertAction(VERIFY_CODE, UiText.text("Verify"), enabled = false, screenId = SCREEN)
        UiContract.assertActionStates(RESEND_CODE, listOf(
            org.hamcrest.Matchers.equalTo(UiText.text("ResendCode")) to true,
            UiText.numericTemplate("ResendCodeSeconds") to false
        ), SCREEN)
        UiContract.assertIcon(BACK, enabled = true, screenId = SCREEN)
    }

    fun goBack() {
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withAutomationId(BACK, SCREEN))
                .perform(click())
        }
    }

    fun enterVerificationCode(verificationCode: String) {
        VerificationCodeActions.enter(verificationCode)
    }

    /** Requires the real coordinator to skip email after a server-confirmed temporary OTP. */
    fun verifyTemporaryPhoneCode() {
        ScreenWaiter.waitUntilReady(VERIFY_CODE)
        UiContract.assertAction(VERIFY_CODE, UiText.text("Verify"), enabled = true, screenId = SCREEN)
        onView(withAutomationId(VERIFY_CODE, SCREEN)).perform(click())
        ScreenWaiter.waitUntilHidden(SCREEN)
        val next = ScreenWaiter.waitForAny(UiIds.ScreenOnboardingEmail, UsernameScreen.SCREEN)
        check(next == UsernameScreen.SCREEN) {
            "FLOW: email verification is required; the server did not establish the expected temporary-phone onboarding path."
        }
    }

    fun verifyCodeAfterRevocation(): String {
        onView(withAutomationId(VERIFY_CODE, SCREEN)).perform(click())
        ScreenWaiter.waitUntilHidden(SCREEN)
        // The phone page can appear briefly while async verification advances onboarding.
        // Only a ready Send code control means a new verification attempt can be started.
        return when (ScreenWaiter.waitForAnyReady(UiIds.ButtonOnboardingFinalizeContinue, UiIds.ButtonOnboardingPhoneSendCode)) {
            UiIds.ButtonOnboardingFinalizeContinue -> SuccessScreen.SCREEN
            else -> PhoneVerificationScreen.SCREEN
        }
    }
    fun verifyCodeAndWaitFor(nextScreen: String) {
        UiContract.assertAction(VERIFY_CODE, UiText.text("Verify"), enabled = true, screenId = SCREEN)
        ScreenWaiter.performActionAndWaitFor(nextScreen) {
            onView(withAutomationId(VERIFY_CODE, SCREEN))
                .perform(click())
        }
    }
}
