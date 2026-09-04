package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestDataHelper
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalNumberAgeGroup

object TestData {
    private const val PHONE_NUMBER_ARGUMENT = "testPhoneNumber"
    private const val PIN_ARGUMENT = "testPin"
    private const val USERNAME_TIMESTAMP_ARGUMENT = "registrationUsernameTimestamp"
    private const val PERSONAL_NUMBER_AGE_GROUP_ARGUMENT = "personalNumberAgeGroup"
    private const val SOCIAL_SECURITY_NUMBER_ARGUMENT = "testSocialSecurityNumber"

    fun phoneNumber(): String {
        val phoneNumber = this.requiredArgument(PHONE_NUMBER_ARGUMENT)

        check(phoneNumber.matches(Regex("\\d+"))) {
            "The test phone number must contain digits only, without country code."
        }

        return phoneNumber
    }

    fun pin(): String {
        val pin = this.requiredArgument(PIN_ARGUMENT)

        check(pin.matches(Regex("\\d{6}"))) {
            "The test PIN must contain exactly six digits."
        }

        return pin
    }

    fun registrationUsername(): String {
        val timestamp = this.requiredArgument(USERNAME_TIMESTAMP_ARGUMENT)

        check(timestamp.matches(Regex("\\d{14}"))) {
            "The registration username timestamp must use yyyyMMddHHmmss format."
        }

        return timestamp
    }

    fun personalIdentity(): PersonalIdentityTestData {
        val arguments = InstrumentationRegistry.getArguments()
        val socialSecurityNumberOverride = arguments.getString(SOCIAL_SECURITY_NUMBER_ARGUMENT)
            ?.trim()
            ?.takeIf(String::isNotEmpty)

        val ageGroup = arguments.getString(PERSONAL_NUMBER_AGE_GROUP_ARGUMENT)
            ?.trim()
            ?.takeIf(String::isNotEmpty)
            ?: PersonalNumberAgeGroup.ADULT_2000S.argumentValue

        return PersonalIdentityTestDataHelper.create(
            ageGroup = PersonalNumberAgeGroup.fromArgument(ageGroup),
            socialSecurityNumberOverride = socialSecurityNumberOverride
        )
    }

    private fun requiredArgument(argumentName: String): String =
        InstrumentationRegistry.getArguments()
            .getString(argumentName)
            ?.takeIf(String::isNotBlank)
            ?: throw IllegalStateException(
                "Missing instrumentation argument '$argumentName'."
            )
}
