package com.tag.neuroaccess.neuroaccessespressoautomationtests.screens

import android.view.View
import android.widget.DatePicker
import android.widget.EditText
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.UiController
import androidx.test.espresso.ViewAction
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.action.ViewActions.closeSoftKeyboard
import androidx.test.espresso.action.ViewActions.replaceText
import androidx.test.espresso.action.ViewActions.swipeUp
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.RootMatchers.isDialog
import androidx.test.espresso.matcher.ViewMatchers.isAssignableFrom
import androidx.test.espresso.matcher.ViewMatchers.isDescendantOfA
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withContentDescription
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.tag.neuroaccess.neuroaccessespressoautomationtests.framework.ScreenWaiter
import com.tag.neuroaccess.neuroaccessespressoautomationtests.helper.PersonalIdentityTestData
import org.hamcrest.Matcher
import org.hamcrest.Matchers.allOf
import org.hamcrest.Matchers.containsString
import java.time.LocalDate

object KycProcessScreen {
    const val SCREEN = "screen_kyc_process"
    const val SUMMARY_SCREEN = "screen_kyc_summary"
    const val APPLICATION_SENT = "panel_kyc_application_sent"

    private const val NEXT = "button_kyc_next"
    private const val BACK = "button_kyc_back"
    private const val PAGE_PREPARE_DOCUMENTS = "screen_kyc_page_prepareDocuments"
    private const val PAGE_PERSONAL_INFORMATION = "screen_kyc_page_personalInfo"
    private const val PAGE_IDENTITY_DOCUMENTS = "screen_kyc_page_identityDocuments"
    private const val PAGE_ADDRESS_INFORMATION = "screen_kyc_page_addressInformation"
    private const val PAGE_SELFIE = "screen_kyc_page_selfiePage"
    private const val PAGE_ORGANIZATION_QUESTION = "screen_kyc_page_isOrgPage"
    private const val PAGE_ORGANIZATION_INFORMATION = "screen_kyc_page_orgInfo"
    private const val FIELD_FIRST_NAME = "field_kyc_first"
    private const val FIELD_MIDDLE_NAME = "field_kyc_middle"
    private const val FIELD_LAST_NAME = "field_kyc_last"
    private const val FIELD_PERSONAL_NUMBER = "field_kyc_pnr"
    private const val FIELD_DATE_OF_BIRTH = "field_kyc_bdate"
    private const val FIELD_NATIONALITY = "field_kyc_nationality"
    private const val FIELD_GENDER = "field_kyc_gender"
    private const val FIELD_DOCUMENT_TYPE = "field_kyc_documenttype"
    private const val FIELD_ZIP_CODE = "field_kyc_zip"
    private const val FIELD_ADDRESS = "field_kyc_addr"
    private const val FIELD_APARTMENT = "field_kyc_addr2"
    private const val FIELD_AREA = "field_kyc_area"
    private const val FIELD_CITY = "field_kyc_city"
    private const val FIELD_REGION = "field_kyc_region"
    private const val CONFIRM_DIALOG_TITLE = "Confirm"
    private const val CONFIRM_DIALOG_ACCEPT = "Yes"
    private const val PIN_POPUP_TITLE = "Enter pin"
    private const val PIN_POPUP_SUBMIT = "Enter"
    private const val MAX_PAGE_SCROLLS = 8

    fun assertDisplayed() {
        ScreenWaiter.waitFor(SCREEN)
        onView(withContentDescription(SCREEN))
            .check(matches(isDisplayed()))
    }

    fun navigateToPersonalInformation() {
        var currentPage = ScreenWaiter.waitForAny(
            PAGE_PREPARE_DOCUMENTS,
            PAGE_PERSONAL_INFORMATION,
            PAGE_IDENTITY_DOCUMENTS,
            PAGE_ADDRESS_INFORMATION,
            PAGE_SELFIE,
            PAGE_ORGANIZATION_QUESTION,
            PAGE_ORGANIZATION_INFORMATION,
            SUMMARY_SCREEN
        )

        if (currentPage == PAGE_PREPARE_DOCUMENTS) {
            this.navigateAndWaitForPage(NEXT, currentPage, PAGE_PERSONAL_INFORMATION)
            return
        }

        while (currentPage != PAGE_PERSONAL_INFORMATION) {
            onView(withContentDescription(BACK))
                .perform(click())
            ScreenWaiter.waitUntilHidden(currentPage)
            currentPage = ScreenWaiter.waitForAny(
                PAGE_PERSONAL_INFORMATION,
                PAGE_IDENTITY_DOCUMENTS,
                PAGE_ADDRESS_INFORMATION,
                PAGE_SELFIE,
                PAGE_ORGANIZATION_QUESTION,
                PAGE_ORGANIZATION_INFORMATION
            )
        }
    }

    fun enterPersonalInformation(data: PersonalIdentityTestData) {
        this.replaceField(FIELD_FIRST_NAME, data.firstName)
        this.replaceOptionalField(FIELD_MIDDLE_NAME, data.middleName)
        this.replaceField(FIELD_LAST_NAME, data.lastName)
        this.replaceField(FIELD_PERSONAL_NUMBER, data.personalNumber)
        this.selectDate(FIELD_DATE_OF_BIRTH, data.dateOfBirth)
        this.selectCountry(data.nationality)
        this.selectGender(data.gender.displayName)
    }

    fun continueFromPersonalInformation() {
        this.navigateAndWaitForPage(
            NEXT,
            PAGE_PERSONAL_INFORMATION,
            PAGE_IDENTITY_DOCUMENTS
        )
    }

    fun selectNoIdentityDocumentAndContinue() {
        this.selectPickerValue(FIELD_DOCUMENT_TYPE, "None")
        this.navigateAndWaitForPage(
            NEXT,
            PAGE_IDENTITY_DOCUMENTS,
            PAGE_ADDRESS_INFORMATION
        )
    }

    fun enterAddressAndContinue(data: PersonalIdentityTestData) {
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_ZIP_CODE, data.zipCode)
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_ADDRESS, data.address)
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_APARTMENT, data.apartment)
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_AREA, data.area)
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_CITY, data.city)
        this.replaceFieldOnPage(PAGE_ADDRESS_INFORMATION, FIELD_REGION, data.region)
        this.navigateAndWaitForPage(
            NEXT,
            PAGE_ADDRESS_INFORMATION,
            PAGE_SELFIE
        )
    }

    fun waitForSelfiePage() {
        ScreenWaiter.waitFor(PAGE_SELFIE)
    }

    fun continueFromSelfie() {
        this.navigateAndWaitForPage(
            NEXT,
            PAGE_SELFIE,
            PAGE_ORGANIZATION_QUESTION
        )
    }

    fun continueWithoutOrganization() {
        this.navigateAndWaitForPage(
            NEXT,
            PAGE_ORGANIZATION_QUESTION,
            SUMMARY_SCREEN
        )
    }

    private fun navigateAndWaitForPage(
        buttonAutomationId: String,
        currentPageAutomationId: String,
        destinationPageAutomationId: String
    ) {
        ScreenWaiter.waitUntilEnabled(buttonAutomationId)
        onView(withContentDescription(buttonAutomationId))
            .perform(click())
        ScreenWaiter.waitUntilHidden(currentPageAutomationId)
        ScreenWaiter.waitFor(destinationPageAutomationId)
    }

    fun submitFromSummary(pin: String) {
        ScreenWaiter.waitUntilEnabled(NEXT)
        onView(withContentDescription(NEXT))
            .perform(click())

        onView(withText(CONFIRM_DIALOG_TITLE))
            .inRoot(isDialog())
            .check(matches(isDisplayed()))
        onView(withText(CONFIRM_DIALOG_ACCEPT))
            .inRoot(isDialog())
            .perform(click())

        onView(allOf(withText(PIN_POPUP_TITLE), isDisplayed()))
            .check(matches(isDisplayed()))
        onView(allOf(isAssignableFrom(EditText::class.java), isDisplayed()))
            .perform(replaceText(pin), closeSoftKeyboard())

        ScreenWaiter.performActionAndWaitFor(APPLICATION_SENT) {
            onView(allOf(withText(PIN_POPUP_SUBMIT), isDisplayed()))
                .perform(click())
        }
    }

    private fun replaceField(fieldAutomationId: String, value: String) {
        onView(this.editTextWithin(fieldAutomationId))
            .perform(replaceText(value), closeSoftKeyboard())
    }

    private fun replaceOptionalField(fieldAutomationId: String, value: String) {
        if (ScreenWaiter.isDisplayed(fieldAutomationId)) {
            this.replaceField(fieldAutomationId, value)
        }
    }

    private fun replaceFieldOnPage(
        pageAutomationId: String,
        fieldAutomationId: String,
        value: String
    ) {
        this.scrollFieldIntoView(pageAutomationId, fieldAutomationId)
        this.replaceField(fieldAutomationId, value)
    }

    private fun selectPickerValue(fieldAutomationId: String, value: String) {
        onView(this.editTextWithin(fieldAutomationId))
            .perform(click())
        onView(withText(containsString(value)))
            .inRoot(isDialog())
            .perform(click())
        onView(this.editTextWithin(fieldAutomationId))
            .check(matches(withText(containsString(value))))
    }

    private fun selectCountry(countryName: String) {
        onView(this.editTextWithin(FIELD_NATIONALITY))
            .perform(click())
        onView(withText(containsString(countryName)))
            .inRoot(isDialog())
            .perform(click())
    }

    private fun selectGender(genderName: String) {
        this.scrollFieldIntoView(PAGE_PERSONAL_INFORMATION, FIELD_GENDER)
        onView(this.editTextWithin(FIELD_GENDER))
            .perform(click())
        onView(withText(containsString(genderName)))
            .inRoot(isDialog())
            .perform(click())
        onView(this.editTextWithin(FIELD_GENDER))
            .check(matches(withText(containsString(genderName))))
    }

    private fun scrollFieldIntoView(pageAutomationId: String, fieldAutomationId: String) {
        repeat(MAX_PAGE_SCROLLS) {
            if (ScreenWaiter.isDisplayedOnScreen(fieldAutomationId)) {
                return
            }

            onView(withContentDescription(pageAutomationId))
                .perform(swipeUp())
        }

        ScreenWaiter.waitFor(fieldAutomationId)
        onView(withContentDescription(fieldAutomationId))
            .check(matches(isDisplayed()))
    }

    private fun selectDate(fieldAutomationId: String, date: LocalDate) {
        onView(this.editTextWithin(fieldAutomationId))
            .perform(click())
        onView(isAssignableFrom(DatePicker::class.java))
            .perform(SetDateAction(date))
        onView(withText(android.R.string.ok))
            .inRoot(isDialog())
            .perform(click())
    }

    private fun editTextWithin(fieldAutomationId: String): Matcher<View> = allOf(
        isAssignableFrom(EditText::class.java),
        isDescendantOfA(withContentDescription(fieldAutomationId))
    )
}

private class SetDateAction(private val date: LocalDate) : ViewAction {
    override fun getConstraints(): Matcher<View> = isAssignableFrom(DatePicker::class.java)

    override fun getDescription(): String = "set date to $date"

    override fun perform(uiController: UiController, view: View) {
        val datePicker = view as DatePicker
        datePicker.updateDate(this.date.year, this.date.monthValue - 1, this.date.dayOfMonth)
        uiController.loopMainThreadUntilIdle()
    }
}
