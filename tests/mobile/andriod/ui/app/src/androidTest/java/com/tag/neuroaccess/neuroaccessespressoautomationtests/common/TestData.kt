package com.tag.neuroaccess.neuroaccessespressoautomationtests.common

import androidx.test.platform.app.InstrumentationRegistry
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestDataHelper
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalNumberAgeGroup

object TestData {
    private const val PHONE_NUMBER_ARGUMENT = "testPhoneNumber"
    private const val PIN_ARGUMENT = "testPin"
    private const val NEW_PIN_ARGUMENT = "testNewPin"
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

    fun newPin(): String {
        val newPin = this.requiredArgument(NEW_PIN_ARGUMENT)

        check(newPin.matches(Regex("\\d{6}"))) {
            "The new test PIN must contain exactly six digits."
        }
        check(newPin != this.pin()) {
            "The new test PIN must differ from the current test PIN."
        }

        return newPin
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

    /**
     * Reads the Quick Login page URL supplied by the local test configuration.
     * @return An HTTPS page URL without embedded credentials.
     */
    fun quickLoginPageUrl(): String {
        val configuredUrl = this.requiredArgument("testQuickLoginPageUrl")
        return try {
            java.net.URI(configuredUrl).also {
                require(it.scheme == "https" && !it.host.isNullOrBlank() && it.rawUserInfo == null)
            }.toASCIIString()
        } catch (_: Exception) {
            error("testQuickLoginPageUrl must be a valid HTTPS URL without credentials.")
        }
    }

    /** @return The Quick Login API endpoint on the configured page's server. */
    fun quickLoginEndpoint(): String =
        java.net.URI(this.quickLoginPageUrl()).resolve("/QuickLogin").toASCIIString()

    private fun requiredArgument(argumentName: String): String =
        InstrumentationRegistry.getArguments()
            .getString(argumentName)
            ?.takeIf(String::isNotBlank)
            ?: throw IllegalStateException(
                "Missing instrumentation argument '$argumentName'."
            )
}
