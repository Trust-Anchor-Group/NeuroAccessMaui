package com.tag.neuroaccess.neuroaccessespressoautomationtests.options

import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.SupportedLanguage
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.LanguageSelectionPopup
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class LanguageOptionsFlowTest : BaseTest() {

    @Test
    fun selectLanguageForColdStartVerification() {
        val language = this.requestedLanguageOrSkip()

        IdProviderScreen.assertDisplayed()
        val currentLanguage = IdProviderScreen.currentLanguage()
        if (currentLanguage.code != language.code) {
            IdProviderScreen.openLanguageSelector(currentLanguage)
            LanguageSelectionPopup.select(language)
        }
        IdProviderScreen.assertLanguage(language)
    }

    @Test
    fun verifyLanguageAfterColdStart() {
        val language = this.requestedLanguageOrSkip()

        IdProviderScreen.assertDisplayed()
        IdProviderScreen.assertLanguage(language)
    }

    private fun requestedLanguageOrSkip(): SupportedLanguage {
        val languageCode = InstrumentationRegistry.getArguments()
            .getString(LANGUAGE_CODE_ARGUMENT)
        assumeTrue(
            "Run this phase through the languageOptionsColdStartTest Gradle task.",
            !languageCode.isNullOrBlank()
        )

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
