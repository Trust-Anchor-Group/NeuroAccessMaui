package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

/** A supported locale and its independently maintained visible language label. */
data class SupportedLanguage(val code: String, val nativeName: String) {
    /** Exact welcome heading from the test asset. */
    val identityProviderTitle: String get() = UiText.text(this, "ActivateYourDigitalIdentity")

    /** Exact invitation prompt from the test asset. */
    val inviteCodeQuestion: String get() = UiText.text(this, "HaveAnInviteCodeQuestion")

    /** Exact provider-selection button label from the test asset. */
    val selectForMe: String get() = UiText.text(this, "SelectForMe")

    /** Exact title of the language-change confirmation. */
    val languageChangedTitle: String get() = UiText.text(this, "LanguageChangedTitle")

    companion object {
        /** All twelve production-supported locales with the app's explicit display labels. */
        val all = listOf(
            SupportedLanguage("en", "English"),
            SupportedLanguage("sv", "Svenska"),
            SupportedLanguage("es", "Español"),
            SupportedLanguage("fr", "Français"),
            SupportedLanguage("de", "Deutsch"),
            SupportedLanguage("da", "Dansk"),
            SupportedLanguage("no", "Norsk"),
            SupportedLanguage("fi", "Suomi"),
            SupportedLanguage("sr", "Српски"),
            SupportedLanguage("pt", "Português"),
            SupportedLanguage("ro", "Română"),
            SupportedLanguage("ru", "Русский")
        )
    }
}
