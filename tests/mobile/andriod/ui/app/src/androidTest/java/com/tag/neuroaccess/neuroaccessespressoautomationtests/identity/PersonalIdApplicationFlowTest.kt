package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import androidx.test.ext.junit.runners.AndroidJUnit4
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.BaseTest
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityPhotoScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.KycProcessScreen
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class PersonalIdApplicationFlowTest : BaseTest() {

    @Test
    fun testUserAppliesForPersonalId() {
        val identity = TestData.personalIdentity()

        HomeScreen.assertDisplayed()
        HomeScreen.openPersonalIdApplications()

        ApplicationsScreen.assertDisplayed()
        ApplicationsScreen.openPersonalIdApplication()

        KycProcessScreen.assertDisplayed()
        KycProcessScreen.navigateToPersonalInformation()
        KycProcessScreen.enterPersonalInformation(identity)
        KycProcessScreen.continueFromPersonalInformation()

        KycProcessScreen.selectNoIdentityDocumentAndContinue()

        KycProcessScreen.enterAddressAndContinue(identity)

        IdentityPhotoScreen.uploadGeneratedTestImage()
        KycProcessScreen.continueFromSelfie()

        KycProcessScreen.continueWithoutOrganization()

        KycProcessScreen.submitFromSummary(TestData.pin())
    }
}
