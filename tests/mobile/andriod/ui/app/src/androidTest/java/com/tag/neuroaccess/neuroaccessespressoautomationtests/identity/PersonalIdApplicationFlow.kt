package com.tag.neuroaccess.neuroaccessespressoautomationtests.identity

import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.HomeScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.IdentityPhotoScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.KycProcessScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.KycSummaryScreen

internal object PersonalIdApplicationFlow {

    fun submit(newApplication: Boolean = false, identity: PersonalIdentityTestData = TestData.personalIdentity()) {


        HomeScreen.assertDisplayed()
        HomeScreen.openPersonalIdApplications()

        submitFromApplications(identity, newApplication)
    }

    fun submitFromApplications(identity: PersonalIdentityTestData, newApplication: Boolean = true) {
        ApplicationsScreen.assertDisplayed()
        if (newApplication) ApplicationsScreen.openNewPersonalIdApplication() else ApplicationsScreen.openPersonalIdApplication()

        KycProcessScreen.assertDisplayed()
        KycProcessScreen.navigateToPersonalInformation()
        val includesMiddleName = KycProcessScreen.enterPersonalInformation(identity)
        KycProcessScreen.continueFromPersonalInformation()

        KycProcessScreen.selectNoIdentityDocumentAndContinue()

        KycProcessScreen.enterAddressAndContinue(identity)

        IdentityPhotoScreen.uploadGeneratedTestImage()
        KycProcessScreen.continueFromSelfie()

        KycProcessScreen.continueWithoutOrganization()

        KycSummaryScreen.assertMatches(identity, includesMiddleName)

        KycProcessScreen.submitFromSummary(TestData.pin())
    }
}
