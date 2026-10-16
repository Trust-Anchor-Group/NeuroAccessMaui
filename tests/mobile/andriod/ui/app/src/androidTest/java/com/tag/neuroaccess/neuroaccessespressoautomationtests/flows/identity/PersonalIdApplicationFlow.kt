package com.tag.neuroaccess.neuroaccessespressoautomationtests.flows.identity

import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.ApplicationsScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.IdentityPhotoScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.KycProcessScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.identity.KycSummaryScreen
import com.tag.neuroaccess.neuroaccessespressoautomationtests.screens.shared.HomeScreen

import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import com.tag.neuroaccess.neuroaccessespressoautomationtests.common.TestData

internal object PersonalIdApplicationFlow {

    fun submit(newApplication: Boolean = true, identity: PersonalIdentityTestData = TestData.personalIdentity()) {


        preflight(newApplication)
        HomeScreen.assertDisplayed()
        HomeScreen.openPersonalIdApplications()

        submitFromApplications(identity, newApplication)
    }

    fun submitFromApplications(identity: PersonalIdentityTestData, newApplication: Boolean = true) {
        preflight(newApplication)
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

    /** Resolves required configuration before opening or replacing an application. */
    private fun preflight(newApplication: Boolean) {
        TestData.pin()
        if (newApplication) {
            TestData.kycTemplateItemId()
            TestData.kycTemplateName()
        }
    }
}
