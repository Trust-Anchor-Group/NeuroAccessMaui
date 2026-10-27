package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiIds

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.AutomationIdMatcher.withAutomationId
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.UiText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options.LanguageSelectionPopup
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.UiContract

object IdProviderScreen {

    const val SCREEN = UiIds.ScreenOnboardingProvider
    private const val SELECT_FOR_ME = UiIds.ButtonOnboardingProviderSelect

    /** Checks the ready provider screen against an independently specified locale. */
    fun assertDisplayed(language: SupportedLanguage = UiText.language) {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertScreen(SCREEN)
        ScreenWaiter.waitUntilReady(SELECT_FOR_ME)
        UiContract.assertAction(SELECT_FOR_ME, language.selectForMe, enabled = true, screenId = SCREEN)
    }

    fun selectForMe() {
        UiContract.assertAction(SELECT_FOR_ME, UiText.text("SelectForMe"), enabled = true, screenId = SCREEN)
        ScreenWaiter.performActionAndWaitFor(PhoneVerificationScreen.SCREEN) {
            onView(withAutomationId(SELECT_FOR_ME, SCREEN))
                .perform(click())
        }
    }

    /** Resolves the locale label only after its unique production control is ready. */
    fun currentLanguage(): SupportedLanguage {
        ScreenWaiter.waitUntilReady(UiIds.ButtonOnboardingRootLanguage, UiIds.ScreenOnboardingRoot)
        val currentLanguageName = UiContract.readVisibleLabel(UiIds.ButtonOnboardingRootLanguage, UiIds.ScreenOnboardingRoot)

        return checkNotNull(
            SupportedLanguage.all.singleOrNull { language ->
                language.nativeName == currentLanguageName
            }
        ) { "UI: current language label is outside the supported locale contract." }
    }

    fun openLanguageSelector(currentLanguage: SupportedLanguage) {
        UiContract.assertAction(UiIds.ButtonOnboardingRootLanguage, currentLanguage.nativeName, enabled = true, screenId = UiIds.ScreenOnboardingRoot)
        onView(withAutomationId(UiIds.ButtonOnboardingRootLanguage, UiIds.ScreenOnboardingRoot)).perform(click())
        ScreenWaiter.waitFor(LanguageSelectionPopup.POPUP)
    }

    /** Selects the configured locale through the real production language picker. */
    fun selectRequestedLanguage() {
        val requested = UiText.language
        val current = currentLanguage()
        if (requested.code != current.code) {
            openLanguageSelector(current)
            LanguageSelectionPopup.select(requested)
        }
        assertLanguage(requested)
    }

    fun assertLanguage(language: SupportedLanguage) {
        ScreenWaiter.waitFor(SCREEN)
        UiContract.assertLabel(UiIds.LabelOnboardingProviderTitle, language.identityProviderTitle, SCREEN)
        UiContract.assertTextWithin(SCREEN, language.inviteCodeQuestion)
        UiContract.assertInput(UiIds.InputOnboardingProviderInvitecode, enabled = true, screenId = SCREEN)
        UiContract.assertAction(SELECT_FOR_ME, language.selectForMe, enabled = true, screenId = SCREEN)
        UiContract.assertAction(UiIds.ButtonOnboardingRootLanguage, language.nativeName, enabled = true, screenId = UiIds.ScreenOnboardingRoot)
    }
}
