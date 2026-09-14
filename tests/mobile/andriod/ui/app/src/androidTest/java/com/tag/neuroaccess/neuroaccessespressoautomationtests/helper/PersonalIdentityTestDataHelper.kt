package com.tag.neuroaccess.neuroaccessespressoautomationtests.helper

import java.time.LocalDate

enum class PersonalNumberAgeGroup(val argumentValue: String) {
    EXTREME_OLD("extreme-old"),
    OLDER_ADULT("older-adult"),
    ADULT_2000S("adult-2000s"),
    INFANT_2026("infant-2026");

    companion object {
        fun fromArgument(argument: String): PersonalNumberAgeGroup =
            entries.firstOrNull { ageGroup -> ageGroup.argumentValue == argument.lowercase() }
                ?: throw IllegalArgumentException(
                    "Unknown personal-number age group '$argument'. Expected one of " +
                        entries.joinToString { it.argumentValue } + "."
                )
    }
}

enum class TestGender(val displayName: String) {
    MALE("Male"),
    FEMALE("Female"),
    OTHER("Other")
}

data class PersonalIdentityTestData(
    val firstName: String,
    val middleName: String,
    val lastName: String,
    val personalNumber: String,
    val dateOfBirth: LocalDate,
    val nationality: String,
    val gender: TestGender,
    val zipCode: String,
    val address: String,
    val apartment: String,
    val area: String,
    val city: String,
    val region: String
)

object PersonalIdentityTestDataHelper {
    private val firstNames = listOf("Alice", "Erik", "Linnea", "Oscar", "Saga", "Viktor")
    private val middleNames = listOf("Maria", "Karl", "Elin", "Nils", "Anna", "Lars")
    private val lastNames = listOf("Andersson", "Berg", "Lindgren", "Nilsson", "Svensson", "Wallin")

    private val identitiesByAgeGroup = mapOf(
        PersonalNumberAgeGroup.EXTREME_OLD to AmericanIdentitySeed(
            socialSecurityNumber = "900010001",
            dateOfBirth = LocalDate.of(1890, 1, 1)
        ),
        PersonalNumberAgeGroup.OLDER_ADULT to AmericanIdentitySeed(
            socialSecurityNumber = "900250002",
            dateOfBirth = LocalDate.of(1925, 1, 1)
        ),
        PersonalNumberAgeGroup.ADULT_2000S to AmericanIdentitySeed(
            socialSecurityNumber = "900000003",
            dateOfBirth = LocalDate.of(2000, 1, 1)
        ),
        PersonalNumberAgeGroup.INFANT_2026 to AmericanIdentitySeed(
            socialSecurityNumber = "900260004",
            dateOfBirth = LocalDate.of(2026, 1, 1)
        )
    )

    fun create(
        ageGroup: PersonalNumberAgeGroup,
        socialSecurityNumberOverride: String? = null
    ): PersonalIdentityTestData {
        val identitySeed = checkNotNull(this.identitiesByAgeGroup[ageGroup])
        val socialSecurityNumber = socialSecurityNumberOverride ?: identitySeed.socialSecurityNumber

        require(socialSecurityNumber.matches(Regex("\\d{9}"))) {
            "The US Social Security number must contain exactly nine digits."
        }

        val selector = socialSecurityNumber.takeLast(4).toInt()
        return PersonalIdentityTestData(
            firstName = this.firstNames[selector % this.firstNames.size],
            middleName = this.middleNames[(selector / 3) % this.middleNames.size],
            lastName = this.lastNames[(selector / 7) % this.lastNames.size],
            personalNumber = socialSecurityNumber,
            dateOfBirth = identitySeed.dateOfBirth,
            nationality = "United States of America",
            gender = TestGender.entries[selector % TestGender.entries.size],
            zipCode = "12345",
            address = "Testgatan 12",
            apartment = "1201",
            area = "Centrum",
            city = "Stockholm",
            region = "Stockholm"
        )
    }
}

private data class AmericanIdentitySeed(
    val socialSecurityNumber: String,
    val dateOfBirth: LocalDate
)
