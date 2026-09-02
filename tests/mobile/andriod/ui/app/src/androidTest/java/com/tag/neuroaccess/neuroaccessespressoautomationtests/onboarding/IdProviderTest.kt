package com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdProviderScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.PhoneVerificationScreen
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
