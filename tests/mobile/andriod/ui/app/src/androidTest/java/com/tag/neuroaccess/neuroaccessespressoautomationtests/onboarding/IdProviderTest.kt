package com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.onboarding.PhoneVerificationScreen

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class IdProviderTest : BaseTest() {

    @Test
    fun selectForMeNavigatesToPhoneVerification() {
        IdProviderScreen.assertDisplayed()
        IdProviderScreen.selectForMe()
        PhoneVerificationScreen.assertDisplayed()
    }
}
