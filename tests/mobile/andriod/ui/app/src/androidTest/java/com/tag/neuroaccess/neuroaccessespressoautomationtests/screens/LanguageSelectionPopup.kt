package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.DeviceActions
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

object LanguageSelectionPopup {
    const val POPUP = "popup_select_language"

    private const val LANGUAGE_CHANGE_DIALOG_DELAY_MILLISECONDS = 5_000L
    private const val LANGUAGE_CHANGE_DIALOG_BOTTOM_OFFSET_PIXELS = 40
    private const val MAXIMUM_SCROLL_ATTEMPTS = 3

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

        DeviceActions.tapFromApplicationBottom(
            bottomOffsetPixels = LANGUAGE_CHANGE_DIALOG_BOTTOM_OFFSET_PIXELS,
            delayMilliseconds = LANGUAGE_CHANGE_DIALOG_DELAY_MILLISECONDS
        )
        ScreenWaiter.waitForIdle()
    }
}
