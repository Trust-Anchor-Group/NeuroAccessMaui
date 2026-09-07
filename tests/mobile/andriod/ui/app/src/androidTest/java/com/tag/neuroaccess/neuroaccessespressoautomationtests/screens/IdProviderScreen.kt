package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import org.hamcrest.Matchers.allOf

object IdProviderScreen {

    const val SCREEN = "screen_onboarding_id_provider"
    private const val SELECT_FOR_ME = "button_id_provider_select_for_me"

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(SELECT_FOR_ME))
            .check(matches(isDisplayed()))
    }

    fun selectForMe() {
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withContentDescription(SELECT_FOR_ME))
                .perform(click())
        }
    }

    fun currentLanguage(): SupportedLanguage {
        val languageNames = SupportedLanguage.all
            .map { language -> language.nativeName }
            .toTypedArray()
        val currentLanguageName = checkNotNull(ScreenWaiter.firstDisplayedEnabledText(*languageNames)) {
            "The current language button label is not displayed."
        }

        return checkNotNull(
            SupportedLanguage.all.firstOrNull { language ->
                language.nativeName == currentLanguageName
            }
        )
    }

    fun openLanguageSelector(currentLanguage: SupportedLanguage) {
        val currentIndex = SupportedLanguage.all.indexOf(currentLanguage)
        val markerIndex = if (currentIndex < SupportedLanguage.all.lastIndex) {
            currentIndex + 1
        } else {
            currentIndex - 1
        }
        val popupMarker = SupportedLanguage.all[markerIndex]

        onView(
            allOf(
                withText(currentLanguage.nativeName),
                isDisplayed(),
                isEnabled()
            )
        ).perform(click())
        ScreenWaiter.waitForText(popupMarker.nativeName)
    }

    fun assertLanguage(language: SupportedLanguage) {
        ScreenWaiter.waitForText(language.identityProviderTitle)
        onView(withText(language.identityProviderTitle)).check(matches(isDisplayed()))
        onView(withText(language.inviteCodeQuestion)).check(matches(isDisplayed()))
        onView(withText(language.selectForMe)).check(matches(isDisplayed()))
    }
}
