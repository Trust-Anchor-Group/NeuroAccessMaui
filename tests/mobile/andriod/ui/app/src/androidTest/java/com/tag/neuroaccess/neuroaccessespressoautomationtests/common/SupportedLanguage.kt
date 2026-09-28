package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

data class SupportedLanguage(
    val code: String,
    val nativeName: String,
    val identityProviderTitle: String,
    val inviteCodeQuestion: String,
    val selectForMe: String,
    val languageChangedTitle: String
) {
    companion object {
        val all = listOf(
            SupportedLanguage("en", "English", "Select your ID provider", "Have an invite code?", "Select for me", "Language changed"),
            SupportedLanguage("sv", "svenska", "Aktivera ditt digitala ID", "Har du en inbjudningskod?", "Välj åt mig", "Språk ändrat"),
            SupportedLanguage("es", "español", "Activa tu identidad digital", "¿Tienes un código de invitación?", "Seleccionar por mí", "Idioma cambiado"),
            SupportedLanguage("fr", "français", "Sélectionnez votre fournisseur d’identification", "Vous avez un code d’invitation ?", "Sélectionnez pour moi", "Langue modifiée"),
            SupportedLanguage("de", "Deutsch", "Wählen Sie Ihren ID-Anbieter aus.", "Haben Sie einen Einladungscode?", "Für mich auswählen", "Sprache geändert"),
            SupportedLanguage("da", "dansk", "Vælg din id-udbyder", "Har du en invitationskode?", "Vælg for mig", "Sproget er ændret"),
            SupportedLanguage("no", "norsk", "Velg ID-leverandør", "Har du en invitasjonskode?", "Velg for meg", "Språk endret"),
            SupportedLanguage("fi", "suomi", "Valitse tunnuksen tarjoaja", "Onko sinulla kutsukoodi?", "Valitse minulle", "Kieli vaihtunut"),
            SupportedLanguage("sr", "српски", "Izaberite svog provajdera ID-a", "Imate pozivni kod?", "Izaberi za mene", "Jezik promenjen"),
            SupportedLanguage("pt", "português", "Ative sua identidade digital", "Tem um código de convite?", "Selecionar por mim", "Idioma alterado"),
            SupportedLanguage("ro", "română", "Selectați furnizorul de ID", "Aveți un cod de invitație?", "Selectează pentru mine", "Limba schimbată"),
            SupportedLanguage("ru", "русский", "Выберите поставщика удостоверений личности", "У вас есть код приглашения?", "Выберите для меня", "Язык изменен")
        )
    }
}
