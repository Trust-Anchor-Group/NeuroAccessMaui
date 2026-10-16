package com.tag.neuroaccess.neuroaccessespressoautomationtests.options

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.options.LanguageSelectionPopup

import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class LanguageOptionsFlowTest : BaseTest() {

    @Test
    fun selectLanguageForColdStartVerification() {
        val language = this.requestedLanguage()

        val currentLanguage = IdProviderScreen.currentLanguage()
        IdProviderScreen.assertDisplayed(currentLanguage)
        IdProviderScreen.assertLanguage(currentLanguage)
        if (currentLanguage.code != language.code) {
            IdProviderScreen.openLanguageSelector(currentLanguage)
            LanguageSelectionPopup.select(language)
        }
        IdProviderScreen.assertLanguage(language)
    }

    @Test
    fun verifyLanguageAfterColdStart() {
        val language = this.requestedLanguage()

        IdProviderScreen.assertDisplayed()
        IdProviderScreen.assertLanguage(language)
    }

    private fun requestedLanguage(): SupportedLanguage {
        val languageCode = InstrumentationRegistry.getArguments()
            .getString(LANGUAGE_CODE_ARGUMENT)
        require(!languageCode.isNullOrBlank()) {
            "Pass languageCode explicitly through the languageOptionsColdStartTest task."
        }

        return checkNotNull(
            SupportedLanguage.all.firstOrNull { language -> language.code == languageCode }
        ) {
            "Unsupported language code: $languageCode"
        }
    }

    private companion object {
        const val LANGUAGE_CODE_ARGUMENT = "languageCode"
    }
}
