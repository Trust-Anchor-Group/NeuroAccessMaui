package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

import androidx.test.platform.app.InstrumentationRegistry
import org.json.JSONObject
import org.hamcrest.Description
import org.hamcrest.Matcher
import org.hamcrest.TypeSafeMatcher

/** Loads independently maintained text expectations from the test APK, not from the app. */
object UiText {
    private val contracts = mutableMapOf<String, JSONObject>()

    /** Requested contract locale; languageCode remains a compatibility alias. */
    val language: SupportedLanguage
        get() {
            val arguments = InstrumentationRegistry.getArguments()
            val current = arguments.getString("testLanguageCode")?.takeIf { it.isNotBlank() }
            val legacy = arguments.getString("languageCode")?.takeIf { it.isNotBlank() }
            require(current == null || legacy == null || current == legacy) {
                "Conflicting testLanguageCode and languageCode arguments."
            }
            val code = current ?: legacy ?: "en"
            return SupportedLanguage.all.singleOrNull { it.code == code }
                ?: throw AssertionError("CONTRACT: unsupported locale '$code'.")
        }

    /** Returns the exact expected string for the requested locale, without runtime fallback. */
    fun text(key: String): String = text(language, key)

    /** Validates the complete independent template without echoing observed values. */
    fun numericTemplate(key: String): Matcher<String> {
        val template = text(key)
        require(template.split("{0}").size == 2) { "CONTRACT: expected one numeric template slot." }
        val pattern = template.split("{0}").joinToString("[0-9]+") { Regex.escape(it) }.toRegex()
        return object : TypeSafeMatcher<String>() {
            override fun describeTo(description: Description) {
                description.appendText("the configured localized numeric template (values redacted)")
            }
            override fun matchesSafely(value: String): Boolean = pattern.matches(value)
        }
    }

    /** Reads the specified locale's test asset and fails when a key has no expectation. */
    @Synchronized
    fun text(language: SupportedLanguage, key: String): String {
        val contract = contracts.getOrPut(language.code) {
            InstrumentationRegistry.getInstrumentation().context.assets
                .open("ui-contracts/${language.code}.json")
                .bufferedReader(Charsets.UTF_8).use { JSONObject(it.readText()) }
        }
        if (!contract.has(key) || contract.isNull(key)) {
            throw AssertionError("CONTRACT: missing '$key' in locale '${language.code}'.")
        }
        return contract.getString(key)
    }
}
