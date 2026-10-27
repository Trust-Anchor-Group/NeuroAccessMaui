package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import android.os.SystemClock
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.scrollTo
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

/** Selects a language and verifies that its native confirmation dialog is dismissed. */
object LanguageSelectionPopup {
    /** Technical identifier of the language selection popup. */
    const val POPUP = UiIds.PopupSettingsLanguage

    private const val DIALOG_TIMEOUT_MILLISECONDS = 15_000L
    private const val DIALOG_POLL_INTERVAL_MILLISECONDS = 100L

    /** Selects [language], confirms the matching dialog and waits for the popup to close. */
    fun select(language: SupportedLanguage) {
        ScreenWaiter.waitFor(POPUP)
        UiContract.assertScreen(POPUP)
        val optionId = UiIds.forKey(UiIds.ItemSettingsLanguageOption, language.code)
        onView(withAutomationId(optionId, POPUP)).perform(scrollTo())
        UiContract.assertAction(optionId, language.nativeName, enabled = true, screenId = POPUP)
        onView(withAutomationId(optionId, POPUP)).perform(click())

        this.waitForConfirmationDialog(language, visible = true)
        val dialog = allOf(isDialog(), withDecorView(hasDescendant(withText(language.languageChangedTitle))))
        onView(withText(language.languageChangedTitle)).inRoot(dialog).check(matches(isDisplayed()))
        onView(withId(android.R.id.message)).inRoot(dialog)
            .check(matches(withText(UiText.text(language, "LanguageChangedMessage"))))
        // MAUI's single-button DisplayAlert uses the negative Android button for dismissal.
        onView(allOf(withId(android.R.id.button2), isDisplayed(), isEnabled()))
            .inRoot(dialog)
            .check(matches(withText(UiText.text(language, "Ok"))))
            .perform(click())
        this.waitForConfirmationDialog(language, visible = false)
        ScreenWaiter.waitUntilHidden(POPUP)
    }

    private fun waitForConfirmationDialog(language: SupportedLanguage, visible: Boolean) {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val deadline = SystemClock.uptimeMillis() + DIALOG_TIMEOUT_MILLISECONDS
        do {
            val root = instrumentation.uiAutomation.rootInActiveWindow
            if (root != null) {
                val titles = root.findAccessibilityNodeInfosByText(language.languageChangedTitle)
                val dialogVisible = try {
                    root.packageName?.toString() == instrumentation.targetContext.packageName &&
                        titles.count { title ->
                            title.isVisibleToUser && title.text?.toString() == language.languageChangedTitle
                        } == 1
                } finally {
                    @Suppress("DEPRECATION")
                    titles.forEach { it.recycle() }
                    @Suppress("DEPRECATION")
                    root.recycle()
                }
                if (dialogVisible == visible) {
                    return
                }
            }
            SystemClock.sleep(DIALOG_POLL_INTERVAL_MILLISECONDS)
        } while (SystemClock.uptimeMillis() < deadline)

        error("Language confirmation '${language.languageChangedTitle}' did not become " +
            (if (visible) "visible" else "hidden") + " within $DIALOG_TIMEOUT_MILLISECONDS ms.")
    }
}
