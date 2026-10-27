package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.SuccessScreen

import android.os.SystemClock
import android.widget.EditText
import android.view.View
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.ViewAction
import androidx.test.espresso.UiController
import androidx.test.espresso.PerformException
import androidx.test.espresso.NoMatchingViewException
import androidx.test.espresso.AmbiguousViewMatcherException
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationIdOrAncestor
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import org.hamcrest.Matchers.allOf
import org.hamcrest.Matcher

object PinCreationScreen {
    const val SCREEN = UiIds.ScreenOnboardingPassword
    private const val NEW_PIN = UiIds.InputOnboardingPasswordValue
    private const val CONFIRM_PIN = UiIds.InputOnboardingPasswordConfirmation
    private const val CREATE_PIN = UiIds.ButtonOnboardingPasswordCreate

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        ScreenWaiter.waitFor(NEW_PIN)
        UiContract.assertScreen(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingPasswordTitle, UiText.text("OnboardingDefinePasswordPageTitle"), SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingPasswordValue, UiText.text("DefinePasswordSectionTitle1"), SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingPasswordConfirmation, UiText.text("DefinePasswordSectionTitle2"), SCREEN)
        UiContract.assertInput(NEW_PIN, enabled = true, screenId = SCREEN)
        UiContract.assertInput(CONFIRM_PIN, enabled = true, screenId = SCREEN)
        UiContract.assertAction(CREATE_PIN, UiText.text("CreatePassword"), enabled = false, screenId = SCREEN)
    }

    fun enterPin(pin: String) {
        UiContract.enterText(NEW_PIN, pin, SCREEN)
        UiContract.enterText(CONFIRM_PIN, pin, SCREEN)
    }

    fun createPinAndWaitForNextStep(expectTemporaryOtpWarning: Boolean = false): String {
        ScreenWaiter.waitUntilReady(CREATE_PIN)
        UiContract.assertAction(CREATE_PIN, UiText.text("CreatePassword"), enabled = true, screenId = SCREEN)
        onView(withAutomationId(CREATE_PIN, SCREEN))
            .perform(click())

        if (expectTemporaryOtpWarning) {
            this.dismissTemporaryOtpWarning()
        }

        return ScreenWaiter.waitForAny(BiometricsScreen.SCREEN, SuccessScreen.SCREEN)
    }

    private fun dismissTemporaryOtpWarning() {
        val title = UiText.text("WarningTitle")
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + 15_000L
        var visible = false
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val titles = root.findAccessibilityNodeInfosByText(title)
                try {
                    visible = root.packageName?.toString() == instrumentation.targetContext.packageName &&
                        titles.count { it.isVisibleToUser && it.text?.toString() == title } == 1
                } finally {
                    @Suppress("DEPRECATION")
                    titles.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
            }
            if (visible) break
            SystemClock.sleep(100L)
        } while (SystemClock.uptimeMillis() < deadline)
        check(visible) { "FLOW: the required temporary-OTP warning was not displayed." }

        val dialog = allOf(isDialog(), withDecorView(hasDescendant(withText(title))))
        onView(withText(title)).inRoot(dialog).check(matches(isDisplayed()))
        onView(withId(android.R.id.message)).inRoot(dialog)
            .check(matches(withText(UiText.text("TestOtpUsed"))))
        onView(withId(android.R.id.button2)).inRoot(dialog)
            .check(matches(allOf(isDisplayed(), isEnabled(), withText(UiText.text("Ok")))))
            .perform(click())
    }

}
