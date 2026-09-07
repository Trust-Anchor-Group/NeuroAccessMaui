package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.os.SystemClock
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.RootMatchers.withDecorView
import androidx.test.espresso.matcher.ViewMatchers.hasDescendant
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.DeviceActions
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

/** Selects a language and verifies that its native confirmation dialog is dismissed. */
object LanguageSelectionPopup {
    /** Technical identifier of the language selection popup. */
    const val POPUP = "popup_select_language"

    private const val DIALOG_TIMEOUT_MILLISECONDS = 15_000L
    private const val DIALOG_POLL_INTERVAL_MILLISECONDS = 100L
    private const val MAXIMUM_SCROLL_ATTEMPTS = 3

    /** Selects [language], confirms the matching dialog and waits for the popup to close. */
    fun select(language: SupportedLanguage) {
        for (attempt in 0 until MAXIMUM_SCROLL_ATTEMPTS) {
            if (ScreenWaiter.firstDisplayedEnabledText(language.nativeName) == language.nativeName) {
                break
            }

            DeviceActions.swipeUpInApplication()
            ScreenWaiter.waitForIdle()
        }

        check(ScreenWaiter.firstDisplayedEnabledText(language.nativeName) == language.nativeName) {
            "Language option '${language.nativeName}' was not visible after scrolling."
        }
        onView(
            allOf(
                withText(language.nativeName),
                isDisplayed(),
                isEnabled()
            )
        ).perform(click())

        this.waitForConfirmationDialog(language, visible = true)
        // MAUI's single-button DisplayAlert uses the negative Android button for dismissal.
        onView(allOf(withId(android.R.id.button2), isDisplayed(), isEnabled()))
            .inRoot(allOf(isDialog(), withDecorView(hasDescendant(withText(language.languageChangedTitle)))))
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
                        titles.any { title ->
                            title.isVisibleToUser && title.text?.toString() == language.languageChangedTitle
                        }
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
