using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using static OpenQA.Selenium.Interactions.WheelInputDevice;
using OpenQA.Selenium.Appium.Interfaces;
using static System.Collections.Specialized.BitVector32;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using System.Net.Http.Headers;
using System.Net;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.BiDi.Modules.Log;


namespace NeuroAccess.UiTests
{
	[TestClass]
    public class ApplicationsTest: BaseTest
    {
		public static Actions actions;
		public static SpecialActions specialActions;
		public static System.Drawing.Size screenSize;

		public static bool AlreadyRunnedConstructorOnce = false;
		private static bool reset = false;
		public ApplicationsTest(): base(reset, nameof(ApplicationsTest)) {

		}

		[ClassInitialize]
		public static void ClassInitialize(TestContext context) {
			actions = new Actions(App);
			specialActions = new SpecialActions(App);
			screenSize = App.Manage().Window.Size;
		}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _0_PrepareForTests() {
		//	App.ExecuteScript("mobile: pressKey", new Dictionary<string, string> { { "keycode", "3" } });
		//	Task.Delay(1000).Wait();
		//	AppiumElement cameraApp =  ScrollUpOrDownAndCustomFindElement(() => FindElementByTextContainedInAttribute("content-desc", "Camera"), false, 10);
		//	cameraApp.Click();

		//	AppiumElement takePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "bottombar_capture"));
		//	takePhotoButton.Click();// A photo is taken so that the test where an image should be added by browsing has an image to choose
		//	Task.Delay(1000).Wait();

		//	App.ActivateApp("com.tag.NeuroAccess");
		//}
		[TestMethod]
		[TestCategory("Android")]
		public void _1_getToApplications_Test() {

			AutoFindElement("MainPage");//Control that the test started in main page
			AppiumElement flyoutIcon = AutoCustomFindElement(() => FindElementByAttribute("content-desc", "Open navigation drawer")); //App.FindElement(By.XPath("//*[@content-desc='Open navigation drawer']"));
			flyoutIcon.Click();
			AppiumElement applications = AutoFindElement("AppShell_ApplicationsMenuItem");
			applications.Click();
			AutoFindElement("ApplicationsPage_GoBackImageButton");//Control that the navigation to applications was succesful
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_0_ClickApplyPersonalIdAndCheckPasswordPopUp_Test() {
			AppiumElement applyPersonalIdButton = AutoFindElement("ApplicationsPage_PersonalIdTemplatedButton");
			applyPersonalIdButton.Click();

			AppiumElement passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");

			string fakePassword = "468468468";
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			passwordEntry.Click();
			var elementsWithTextThatShouldBeWrittenCount = App.FindElements(By.XPath($"//*[contains(@resource-id, 'CheckPasswordPopup_PasswordCompositeEntry')]//*[@text = '{fakePassword}']")).Count;//The count of the elements that have the same text that will be written in the entry later.
																																																																	//Now write in the entry
			actions.SendKeys(fakePassword).Perform();
			//Now test if the entry worked and the text was wrritten in it
			int elementsWithTextAfterCount = App.FindElements(By.XPath($"//*[contains(@resource-id, 'CheckPasswordPopup_PasswordCompositeEntry')]//*[@text = '{fakePassword}']")).Count;
			if (elementsWithTextAfterCount > elementsWithTextThatShouldBeWrittenCount) {
				throw new Exception($"The password isn't hidden by default");
			}
			AppiumElement togglePasswordVisibilityButton = AutoFindElement("CheckPasswordPopup_TogglePasswordVisibilityTemplatedButton");
			togglePasswordVisibilityButton.Click();
			Task.Delay(1000).Wait();

			elementsWithTextAfterCount = App.FindElements(By.XPath($"//*[contains(@resource-id, 'CheckPasswordPopup_PasswordCompositeEntry')]//*[@text = '{fakePassword}']")).Count;
			if (elementsWithTextAfterCount <= elementsWithTextThatShouldBeWrittenCount) {
				throw new Exception($"The password entry doesnt work");
			}


			//TODO: Implement a test where a false password is entered many times when the function to lock the app is fixed
			AppiumElement enterPassword = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
			enterPassword.Click();

			AppiumElement okButtonOnPopup = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "button2"));
			okButtonOnPopup.Click();

			AppiumElement cancelButton = AutoFindElement("CheckPasswordPopup_CancelTextButton");
			cancelButton.Click();

			if (!CustomCheckIfElementDisappeared(() => FindUIElement("CheckPasswordPopup_CancelTextButton"))) {
				throw new Exception("Cancel button didn't work and popup didn't dissappear");
			}

			applyPersonalIdButton = AutoFindElement("ApplicationsPage_PersonalIdTemplatedButton");
			applyPersonalIdButton.Click();

			passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
			passwordEntry.Click();
			//Now write in the real password
			actions.SendKeys("Test11").Perform();

			enterPassword = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
			enterPassword.Click();
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_1_AddImageByCameraInApplyOrganizationalIdForm_Test() {
			//pointerInputDevice2 = new PointerInputDevice(kind, name);
			AppiumElement goToTakePhotoButton = AutoFindElement("ApplyIdPage_TakeCameraPhotoImageButton");
			goToTakePhotoButton.Click();

			try {
				AppiumElement allowOneTimeAccessButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "permission_allow_one_time_button"), 5);
				allowOneTimeAccessButton.Click();
			} catch {
				//If the pop up that asks for permission doesn't get found, it means the permission has already been granted.
			}

			AppiumElement takePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "bottombar_capture"));
			takePhotoButton.Click();

			AppiumElement retakePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "retake_button"));
			retakePhotoButton.Click();

			takePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "bottombar_capture"));
			takePhotoButton.Click();

			AppiumElement cancelButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "cancel_button"));
			cancelButton.Click();

			AppiumElement removePhotoButton;
			try {
				removePhotoButton = AutoFindElement("ApplyIdPage_RemovePhotoImageButton", 3);
			} catch {
				//Reaching here means the remove buttton doesn't exist, which means no image was taken and the cancel button worked
			}
			goToTakePhotoButton = AutoFindElement("ApplyIdPage_TakeCameraPhotoImageButton");
			goToTakePhotoButton.Click();
			takePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "bottombar_capture"));
			takePhotoButton.Click();

			AppiumElement usePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "done_button"));
			usePhotoButton.Click();

			removePhotoButton = AutoFindElement("ApplyIdPage_RemovePhotoImageButton", 3);
			removePhotoButton.Click();

			if (!CustomCheckIfElementDisappeared(() => AutoFindElement("ApplyIdPage_RemovePhotoImageButton", 3))) {
				throw new Exception("Image wasn't removed after remove photo button was clicked");
			}//If it dissappeared then it means the image dissappeared most likely, and the test to remove the image was successfull.
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_2_AddImageByBrowsingInApplyPersonalIdForm_Test() {
			AppiumElement choosePhotoButton = AutoFindElement("ApplyIdPage_ChoosePhotoImageButton");
			choosePhotoButton.Click();

			AppiumElement cancelButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("content-desc", "Cancel"));
			cancelButton.Click();

			choosePhotoButton = AutoFindElement("ApplyIdPage_ChoosePhotoImageButton");
			choosePhotoButton.Click();

			AppiumElement anImage = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("content-desc", "Photo taken"));
			anImage.Click();

			AppiumElement removePhotoButton = AutoFindElement("ApplyIdPage_RemovePhotoImageButton", 3);//By Checking if the removePhoto button exists, one will be able to know if the test succeeded and an image was chosen.
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_3_PersonalInformationInPersonalIdForm_Test() {
			AppiumElement firstNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_FirstNameCompositeEntry", false, 3, specialActions);
			WriteInAndTestComplexEntry("ApplyIdPage_FirstNameCompositeEntry", "Test text 123", firstNameInputField, actions);

			AppiumElement middleNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_MiddleNamesCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_MiddleNamesCompositeEntry", "Test text 123", middleNameInputField,	actions);

			AppiumElement lastNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_LastNamesCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_LastNamesCompositeEntry", "Test text 123", lastNameInputField, actions);

			AppiumElement personalNumberInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_PersonalNumberCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_PersonalNumberCompositeEntry", "123456789", personalNumberInputField, actions);

			AppiumElement adressInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_AddressCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_AddressCompositeEntry", "Test text 123", adressInputField,	actions);

			AppiumElement adress2InputField = ScrollUpOrDownAndFindElement("ApplyIdPage_Address2CompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_Address2CompositeEntry", "Test text 123", adress2InputField, actions);

			AppiumElement zipCodeInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_ZipCodeCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_ZipCodeCompositeEntry", "Test text 123", zipCodeInputField, actions);

			AppiumElement areaInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_AreaCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_AreaCompositeEntry", "Test text 123", areaInputField, actions);

			AppiumElement cityInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_CityCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_CityCompositeEntry", "Test text 123", cityInputField, actions);

			AppiumElement regionInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_RegionCompositeEntry", false, 1, specialActions);//
			WriteInAndTestComplexEntry("ApplyIdPage_RegionCompositeEntry", "Test text 123", regionInputField, actions);
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_4_NationalityPickerInApplyPersonalIdForm_Test() {
			AppiumElement nationalityPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectNationalityCompositePicker", false, 1, specialActions, screenSize.Width / 2);//
			nationalityPicker.Click();
			var usaCode = AutoCustomFindElement(() => FindElementByTextContaintedInText("🇺🇸"));
			usaCode.Click();
			var nationalityPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectNationalityCompositePicker')]//*[contains(@text, '🇺🇸')]")), 2);//Here it's checked if changing the nationality actually worked
			nationalityPickerTextElement.Click();
			var arubaCode = ScrollUpOrDownAndCustomFindElement(() => FindElementByTextContaintedInText("🇦🇼"), false, 10, specialActions, screenSize.Width / 2);
			arubaCode.Click();
			nationalityPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectNationalityCompositePicker')]//*[contains(@text, '🇦🇼')]")), 2);//Here it's checked if changing the nationality actually worked
			nationalityPickerTextElement.Click();
			var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
			cancelButton.Click();
			if (!CustomCheckIfElementDisappeared(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")), 0.5)) {
				throw new Exception("The nationality picker cancel button didn't work, and the nationality picker view didn't dissappear");
			}
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_5_GednderPickerInApplyPersonalIdForm_Test() {
			AppiumElement genderPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectGenderCompositePicker", false, 1, specialActions);//
			genderPicker.Click();
			var MaleGenderButton = AutoCustomFindElement(() => FindElementByTextContaintedInText("♂"));
			MaleGenderButton.Click();
			var genderPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectGenderCompositePicker')]//*[contains(@text, '♂')]")), 2);//Here it's checked if changing the gender actually worked
			genderPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectGenderCompositePicker", false, 1, specialActions);//
			genderPicker.Click();
			var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
			cancelButton.Click();
			if (!CustomCheckIfElementDisappeared(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")), 0.5)) {
				throw new Exception("The gender picker cancel button didn't work, and the gender picker view didn't dissappear");
			}
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_6_DatePickerInApplyOrganizationalIdForm_Test() {
			var screenSize = App.Manage().Window.Size;
			string[] shortMonths = new string[] {
				"Jan", "Feb", "Mar", "Apr", "May", "Jun",
				"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
			string[] months = new string[] {
				"January", "February", "March", "April",
				"May", "June", "July", "August",
				"September", "October", "November", "December"};

			string defaultBirthDate;
			var birthDatePickerTextElement = ScrollUpOrDownAndCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")), false, 3, specialActions);
			defaultBirthDate = birthDatePickerTextElement.Text;
			birthDatePickerTextElement.Click();

			//this part will be to test if the cancel button works
			var chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
			chooseYearButton.Click();
			var defaultYearMinusOne = AutoCustomFindElement(() => FindElementByText("" + (int.Parse(chooseYearButton.GetAttribute("text")) - 1)));//The reason the year just before the default years is searched for, is to make sure it was navigated to view where one can choose the year.
			defaultYearMinusOne.Click();
			var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
			cancelButton.Click();
			Task.Delay(1000).Wait();
			birthDatePickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")));
			if (birthDatePickerTextElement.Text != defaultBirthDate) {//There is no need to test if the DatePicker view dissappeared here since the birthDatePickerTextElement only appears if it dissappeared.
				throw new Exception("The canel button of the datepicker didn't work. The date was changed even though it's supposed not to change if the cancel button is pressed");
			}
			////////////////////////////////

			birthDatePickerTextElement.Click();
			chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
			chooseYearButton.Click();
			AutoCustomFindElement(() => FindElementByText("" + (int.Parse(chooseYearButton.GetAttribute("text")) - 1)));//The reason the year just before the default years is searched for, is to make sure it was navigated to view where one can choose the year.
			var year1999 = ScrollUpOrDownAndCustomFindElement(() => App.FindElement(By.XPath($"//*[@text = '1999' and not(contains(@resource-id, 'date_picker_header_year'))]")), true, 10, specialActions, screenSize.Width / 2);//If the year 1999 is already picked, then only the text being 1999 isn't enough because the date_picker_header_year (the choose year button essentially) will also have the text 1999. So the view also have to not be the date_picker_header_year, and this "not(contains(@resource-id, 'date_picker_header_year')" does the trick.
			year1999.Click();//Here it's tested if changing birth year works
			Task.Delay(1000);
			chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
			if (chooseYearButton.GetAttribute("text") != "1999") {//Here it's checked if the year was actually changed.
				throw new Exception("The test to change the year from the default year to 1999 didn't work. The year didn't change");
			}
			var dateHeader = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_date"));
			AppiumElement? dayToChoose = null;
			int chosenDayNumber = 0;
			int chosenMonthIndex = 0;
			for (int i = 0; i < shortMonths.Count(); i++) {
				if (dateHeader.GetAttribute("text").Contains(shortMonths[i])) {//To test if changing the birth day works, first it's checked which month currently is selected. And then with it the day 10 or 11 is searched for. The days in this datepicker view have their content-desc attribute being "{the day number} {the month} {the year}", so one can get any day button by knowing the month and the year.
					chosenMonthIndex = i;
					dayToChoose = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"10 {months[i]} 1999"), 2);
					if (dayToChoose.GetAttribute("selected") == "false") {//
						chosenDayNumber = 10;
						dayToChoose.Click();
						Task.Delay(1000).Wait();
					} else {
						chosenDayNumber = 11;
						dayToChoose = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"11 {months[i]} 1999"), 2);
						dayToChoose.Click();
						Task.Delay(1000).Wait();
					}
				}
			}
			if (dayToChoose?.GetAttribute("selected") == "false" || !dateHeader.GetAttribute("text").Contains("" + chosenDayNumber)) {
				throw new Exception("The test to change birth day didn't work. The day wasn't changed");
			}
			AppiumElement nextMonthButton = AutoCustomFindElement(() => FindElementByAttribute("content-desc", "Next month"), 2);//this part of the test is to test if changing to the day 12 of the next month works
			nextMonthButton.Click();
			AppiumElement nextMonthDay12;
			if (chosenMonthIndex == months.Count() - 1) {
				chosenMonthIndex = 0;
				nextMonthDay12 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"12 {months[0]} 2000"), 2);
			} else {
				nextMonthDay12 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"12 {months[chosenMonthIndex + 1]} 1999"), 2);
				chosenMonthIndex += 1;
			}
			nextMonthDay12.Click();
			if (nextMonthDay12?.GetAttribute("selected") == "false" || !dateHeader.GetAttribute("text").Contains("" + 12) || !dateHeader.GetAttribute("text").Contains("" + 12)) {
				throw new Exception("The test to change birth day and month didn't work. The day and month wern't changed");
			}
			AppiumElement previousMonthButton = AutoCustomFindElement(() => FindElementByAttribute("content-desc", "Previous month"), 2);//this part of the test is to test if changing to the day 13 of the previous month works
			previousMonthButton.Click();
			AppiumElement prevMonthDay13;
			if (chosenMonthIndex == 0) {
				chosenMonthIndex = 11;//It's 11 but still it's december since arrays indexes start with 0
				prevMonthDay13 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"13 {months[chosenMonthIndex]} 1999"), 2);
			} else {
				chosenMonthIndex -= 1;
				prevMonthDay13 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"13 {months[chosenMonthIndex]} 1999"), 2);
			}
			prevMonthDay13.Click();
			var okButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button1')]")));
			okButton.Click();
			birthDatePickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")));
			if (birthDatePickerTextElement.Text == defaultBirthDate) {
				throw new Exception("The DatePicker view ok button didn't work. The date wasn't changed and it was supposed to change.");
			}
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_7_CheckBoxesInApplyPersonalIdForm_Test() {
			AppiumElement personalInfoUseConsentCheckBox = ScrollUpOrDownAndFindElement("ApplyIdPage_PersonalInformationUseConsentCheckBox", false, 4, specialActions);
			AppiumElement isInformationCorrectCheckBox = ScrollUpOrDownAndFindElement("ApplyIdPage_IsInformationCorrectCheckBox", false, 4, specialActions);

			if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "true" || isInformationCorrectCheckBox.GetAttribute("checked") == "true") {
				throw new Exception("One of the checkboxes were checked by default");
			}

			personalInfoUseConsentCheckBox.Click();
			isInformationCorrectCheckBox.Click();

			if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "false" || isInformationCorrectCheckBox.GetAttribute("checked") == "false") {
				throw new Exception("One of the checkboxes didn't get checked after click");
			}

			personalInfoUseConsentCheckBox.Click();
			isInformationCorrectCheckBox.Click();

			if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "true" || isInformationCorrectCheckBox.GetAttribute("checked") == "true") {
				throw new Exception("One of the checkboxes didn't get unchecked after click");
			}

			personalInfoUseConsentCheckBox.Click();
			isInformationCorrectCheckBox.Click();

			if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "false" || isInformationCorrectCheckBox.GetAttribute("checked") == "false") {
				throw new Exception("One of the checkboxes didn't get checked after click");
			}
		}
		[TestMethod]
		[TestCategory("Android")]
		public void _2_8_SendPersonalIdApplications_Test() {
			AppiumElement sendApplicationButton = AutoFindElement("ApplyIdPage_SendApplicationButton");
			sendApplicationButton.Click();

			AppiumElement cancelSendButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "button2"));
			cancelSendButton.Click();
			if (!CustomCheckIfElementDisappeared(() => FindElementByTextContainedInAttribute("resource-id", "button1"))) {
				throw new Exception("cancel send application button doesn't work and popup didn't dissappear");
			}
			sendApplicationButton = AutoFindElement("ApplyIdPage_SendApplicationButton");
			sendApplicationButton.Click();

			AppiumElement confirmSendButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "button1"));
			confirmSendButton.Click();

			AppiumElement passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
			passwordEntry.Click();
			//Now write in the real password
			actions.SendKeys("Test11").Perform();

			AppiumElement enterPassword = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
			enterPassword.Click();

			//If this appears it means the applications was sent and the test was successfull.
			ScrollUpOrDownAndFindElement("ApplyIdPage_SecondScanQrCodeTextButton", false, 4, specialActions);
		}
		#region Apply for organisational id tests. It's finished but a feature to reset the app and register after applying for personal id needs to be implemented, since one cannot apply for an organisational id after sending it.

		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_0_GetToApplyOrganizationalIdForm_Test() {//ApplyIdPage_GoBackImageButton
		//	AppiumElement goBackFromPersonalIdApplicationFormButton = ScrollUpOrDownAndFindElement("ApplyIdPage_GoBackImageButton", true, 20, specialActions);
		//	goBackFromPersonalIdApplicationFormButton.Click();

		//	AppiumElement organizationIdButton = ScrollUpOrDownAndFindElement("ApplicationsPage_OrganizationalIdTemplatedButton", false, 3, specialActions);
		//	organizationIdButton.Click();

		//	//AppiumElement passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
		//	//passwordEntry.Click();

		//	//this.actions.SendKeys("Test11").Perform();

		//	//AppiumElement enterPassword = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
		//	//enterPassword.Click();

		//	Task.Delay(3000).Wait();
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_1_AddImageByCameraInApplyOrganizationalIdForm_Test() {
		//	//pointerInputDevice2 = new PointerInputDevice(kind, name);
		//	AppiumElement goToTakePhotoButton = AutoFindElement("ApplyIdPage_TakeCameraPhotoImageButton");
		//	goToTakePhotoButton.Click();

		//	try {
		//		AppiumElement allowOneTimeAccessButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "permission_allow_one_time_button"), 5);
		//		allowOneTimeAccessButton.Click();
		//	} catch {
		//		//If the pop up that asks for permission doesn't get found, it means the permission has already been granted.
		//	}

		//	AppiumElement takePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "bottombar_capture"));
		//	takePhotoButton.Click();

		//	AppiumElement usePhotoButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "done_button"));
		//	usePhotoButton.Click();

		//	AutoFindElement("ApplyIdPage_RemovePhotoImageButton", 3);//By Checking if the removePhoto button exists, one will be able to know if the test succeeded and an image was chosen.
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_2_PersonalInformationInApplyOrganizationalIdForm_Test() {
		//	AppiumElement firstNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_FirstNameCompositeEntry", false, 3, specialActions);
		//	WriteInAndTestComplexEntry("ApplyIdPage_FirstNameCompositeEntry", "Test text 123", firstNameInputField, actions);

		//	AppiumElement middleNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_MiddleNamesCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_MiddleNamesCompositeEntry", "Test text 123", middleNameInputField, actions);

		//	AppiumElement lastNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_LastNamesCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_LastNamesCompositeEntry", "Test text 123", lastNameInputField, actions);

		//	AppiumElement personalNumberInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_PersonalNumberCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_PersonalNumberCompositeEntry", "Test text 123", personalNumberInputField, actions);

		//	AppiumElement adressInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_AddressCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_AddressCompositeEntry", "Test text 123", adressInputField, actions);

		//	AppiumElement adress2InputField = ScrollUpOrDownAndFindElement("ApplyIdPage_Address2CompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_Address2CompositeEntry", "Test text 123", adress2InputField, actions);

		//	AppiumElement zipCodeInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_ZipCodeCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_ZipCodeCompositeEntry", "Test text 123", zipCodeInputField, actions);

		//	AppiumElement areaInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_AreaCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_AreaCompositeEntry", "Test text 123", areaInputField, actions);

		//	AppiumElement cityInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_CityCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_CityCompositeEntry", "Test text 123", cityInputField, actions);

		//	AppiumElement regionInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_RegionCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_RegionCompositeEntry", "Test text 123", regionInputField, actions);
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_3_NationalityPickerInApplyOrganizationalIdForm_Test() {
		//	AppiumElement nationalityPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectNationalityCompositePicker", false, 1, specialActions, screenSize.Width / 2);//
		//	nationalityPicker.Click();
		//	var usaCode = AutoCustomFindElement(() => FindElementByTextContaintedInText("🇺🇸"));
		//	usaCode.Click();
		//	var nationalityPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectNationalityCompositePicker')]//*[contains(@text, '🇺🇸')]")), 2);//Here it's checked if changing the nationality actually worked
		//	nationalityPickerTextElement.Click();
		//	var arubaCode = ScrollUpOrDownAndCustomFindElement(() => FindElementByTextContaintedInText("🇦🇼"), false, 10, specialActions, screenSize.Width / 2);
		//	arubaCode.Click();
		//	nationalityPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectNationalityCompositePicker')]//*[contains(@text, '🇦🇼')]")), 2);//Here it's checked if changing the nationality actually worked
		//	nationalityPickerTextElement.Click();
		//	var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
		//	cancelButton.Click();
		//	if (!CustomCheckIfElementDisappeared(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")), 0.5)) {
		//		throw new Exception("The nationality picker cancel button didn't work, and the nationality picker view didn't dissappear");
		//	}
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_4_GednderPickerInApplyOrganizationalIdForm_Test() {
		//	AppiumElement genderPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectGenderCompositePicker", false, 1, specialActions);//
		//	genderPicker.Click();
		//	var MaleGenderButton = AutoCustomFindElement(() => FindElementByTextContaintedInText("♂"));
		//	MaleGenderButton.Click();
		//	var genderPickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath($"//*[contains(@resource-id, 'ApplyIdPage_SelectGenderCompositePicker')]//*[contains(@text, '♂')]")), 2);//Here it's checked if changing the gender actually worked
		//	genderPicker = ScrollUpOrDownAndFindElement("ApplyIdPage_SelectGenderCompositePicker", false, 1, specialActions);//
		//	genderPicker.Click();
		//	var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
		//	cancelButton.Click();
		//	if (!CustomCheckIfElementDisappeared(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")), 0.5)) {
		//		throw new Exception("The gender picker cancel button didn't work, and the gender picker view didn't dissappear");
		//	}
		//}

		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_5_DatePickerInApplyOrganizationalIdForm_Test() {
		//	var screenSize = App.Manage().Window.Size;
		//	string[] shortMonths = new string[] {
		//		"Jan", "Feb", "Mar", "Apr", "May", "Jun",
		//		"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
		//	string[] months = new string[] {
		//		"January", "February", "March", "April",
		//		"May", "June", "July", "August",
		//		"September", "October", "November", "December"};

		//	string defaultBirthDate;
		//	var birthDatePickerTextElement = ScrollUpOrDownAndCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")), false, 3, specialActions);
		//	defaultBirthDate = birthDatePickerTextElement.Text;
		//	birthDatePickerTextElement.Click();

		//	//this part will be to test if the cancel button works
		//	var chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
		//	chooseYearButton.Click();
		//	var defaultYearMinusOne = AutoCustomFindElement(() => FindElementByText("" + (int.Parse(chooseYearButton.GetAttribute("text")) - 1)));//The reason the year just before the default years is searched for, is to make sure it was navigated to view where one can choose the year.
		//	defaultYearMinusOne.Click();
		//	var cancelButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button2')]")));
		//	cancelButton.Click();
		//	Task.Delay(1000).Wait();
		//	birthDatePickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")));
		//	if (birthDatePickerTextElement.Text != defaultBirthDate) {//There is no need to test if the DatePicker view dissappeared here since the birthDatePickerTextElement only appears if it dissappeared.
		//		throw new Exception("The canel button of the datepicker didn't work. The date was changed even though it's supposed not to change if the cancel button is pressed");
		//	}
		//	////////////////////////////////

		//	birthDatePickerTextElement.Click();
		//	chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
		//	chooseYearButton.Click();
		//	AutoCustomFindElement(() => FindElementByText("" + (int.Parse(chooseYearButton.GetAttribute("text")) - 1)));//The reason the year just before the default years is searched for, is to make sure it was navigated to view where one can choose the year.
		//	var year1999 = ScrollUpOrDownAndCustomFindElement(() => App.FindElement(By.XPath($"//*[@text = '1999' and not(contains(@resource-id, 'date_picker_header_year'))]")), true, 10, specialActions, screenSize.Width / 2);//If the year 1999 is already picked, then only the text being 1999 isn't enough because the date_picker_header_year (the choose year button essentially) will also have the text 1999. So the view also have to not be the date_picker_header_year, and this "not(contains(@resource-id, 'date_picker_header_year')" does the trick.
		//	year1999.Click();//Here it's tested if changing birth year works
		//	Task.Delay(1000);
		//	chooseYearButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_year"));
		//	if (chooseYearButton.GetAttribute("text") != "1999") {//Here it's checked if the year was actually changed.
		//		throw new Exception("The test to change the year from the default year to 1999 didn't work. The year didn't change");
		//	}
		//	var dateHeader = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "date_picker_header_date"));
		//	AppiumElement? dayToChoose = null;
		//	int chosenDayNumber = 0;
		//	int chosenMonthIndex = 0;
		//	for (int i = 0; i < shortMonths.Count(); i++) {
		//		if (dateHeader.GetAttribute("text").Contains(shortMonths[i])) {//To test if changing the birth day works, first it's checked which month currently is selected. And then with it the day 10 or 11 is searched for. The days in this datepicker view have their content-desc attribute being "{the day number} {the month} {the year}", so one can get any day button by knowing the month and the year.
		//			chosenMonthIndex = i;
		//			dayToChoose = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"10 {months[i]} 1999"), 2);
		//			if (dayToChoose.GetAttribute("selected") == "false") {//
		//				chosenDayNumber = 10;
		//				dayToChoose.Click();
		//				Task.Delay(1000).Wait();
		//			} else {
		//				chosenDayNumber = 11;
		//				dayToChoose = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"11 {months[i]} 1999"), 2);
		//				dayToChoose.Click();
		//				Task.Delay(1000).Wait();
		//			}
		//		}
		//	}
		//	if (dayToChoose?.GetAttribute("selected") == "false" || !dateHeader.GetAttribute("text").Contains("" + chosenDayNumber)) {
		//		throw new Exception("The test to change birth day didn't work. The day wasn't changed");
		//	}
		//	AppiumElement nextMonthButton = AutoCustomFindElement(() => FindElementByAttribute("content-desc", "Next month"), 2);//this part of the test is to test if changing to the day 12 of the next month works
		//	nextMonthButton.Click();
		//	AppiumElement nextMonthDay12;
		//	if (chosenMonthIndex == months.Count() - 1) {
		//		chosenMonthIndex = 0;
		//		nextMonthDay12 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"12 {months[0]} 2000"), 2);
		//	} else {
		//		nextMonthDay12 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"12 {months[chosenMonthIndex + 1]} 1999"), 2);
		//		chosenMonthIndex += 1;
		//	}
		//	nextMonthDay12.Click();
		//	if (nextMonthDay12?.GetAttribute("selected") == "false" || !dateHeader.GetAttribute("text").Contains("" + 12) || !dateHeader.GetAttribute("text").Contains("" + 12)) {
		//		throw new Exception("The test to change birth day and month didn't work. The day and month wern't changed");
		//	}
		//	AppiumElement previousMonthButton = AutoCustomFindElement(() => FindElementByAttribute("content-desc", "Previous month"), 2);//this part of the test is to test if changing to the day 13 of the previous month works
		//	previousMonthButton.Click();
		//	AppiumElement prevMonthDay13;
		//	if (chosenMonthIndex == 0) {
		//		chosenMonthIndex = 11;//It's 11 but still it's december since arrays indexes start with 0
		//		prevMonthDay13 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"13 {months[chosenMonthIndex]} 1999"), 2);
		//	} else {
		//		chosenMonthIndex -= 1;
		//		prevMonthDay13 = AutoCustomFindElement(() => FindElementByAttribute("content-desc", $"13 {months[chosenMonthIndex]} 1999"), 2);
		//	}
		//	prevMonthDay13.Click();
		//	var okButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'button1')]")));
		//	okButton.Click();
		//	birthDatePickerTextElement = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'ApplyIdPage_SelectBirthDateCompositePicker')]//*[contains(@text, '/')]")));
		//	if (birthDatePickerTextElement.Text == defaultBirthDate) {
		//		throw new Exception("The DatePicker view ok button didn't work. The date wasn't changed and it was supposed to change.");
		//	}
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_6_PersonalInformationInApplyOrganizationalIdForm_Test() {
		//	AppiumElement orgNameInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationNameCompositeEntry", false, 3, specialActions);
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationNameCompositeEntry", "Test text 123", orgNameInputField, actions);

		//	AppiumElement orgDepartmentInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationDepartmentCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationDepartmentCompositeEntry", "Test text 123", orgDepartmentInputField, actions);

		//	AppiumElement orgRoleInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationRoleCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationRoleCompositeEntry", "Test text 123", orgRoleInputField, actions);

		//	AppiumElement orgNumberInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationNumberCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationNumberCompositeEntry", "Test text 123", orgNumberInputField, actions);

		//	AppiumElement orgAdressInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationAddressCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationAddressCompositeEntry", "Test text 123", orgAdressInputField, actions);

		//	AppiumElement orgAdress2InputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationAddress2CompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationAddress2CompositeEntry", "Test text 123", orgAdress2InputField, actions);

		//	AppiumElement orgZipCodeInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationZipCodeCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationZipCodeCompositeEntry", "Test text 123", orgZipCodeInputField, actions);

		//	AppiumElement orgAreaInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationAreaCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationAreaCompositeEntry", "Test text 123", orgAreaInputField, actions);

		//	AppiumElement orgCityInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationCityCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationCityCompositeEntry", "Test text 123", orgCityInputField, actions);

		//	AppiumElement orgRegionInputField = ScrollUpOrDownAndFindElement("ApplyIdPage_OrganizationRegionCompositeEntry", false, 1, specialActions);//
		//	WriteInAndTestComplexEntry("ApplyIdPage_OrganizationRegionCompositeEntry", "Test text 123", orgRegionInputField,	actions);
		//}
		//[TestMethod]
		//[TestCategory("Android")]
		//public void _3_7_CheckBoxesInApplyPersonalIdForm_Test() {// The reason this test ís shorter than the one in applypersonal id is because is the same page for both so same don't have to be tested twice
		//	AppiumElement personalInfoUseConsentCheckBox = AutoFindElement("ApplyIdPage_PersonalInformationUseConsentCheckBox");
		//	AppiumElement isInformationCorrectCheckBox = AutoFindElement("ApplyIdPage_IsInformationCorrectCheckBox");

		//	if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "true" || isInformationCorrectCheckBox.GetAttribute("checked") == "true") {
		//		throw new Exception("One of the checkboxes were checked by default");
		//	}

		//	personalInfoUseConsentCheckBox.Click();
		//	isInformationCorrectCheckBox.Click();

		//	if (personalInfoUseConsentCheckBox.GetAttribute("checked") == "false" || isInformationCorrectCheckBox.GetAttribute("checked") == "false") {
		//		throw new Exception("One of the checkboxes didn't get checked after click");
		//	}

		//	AppiumElement sendApplicationButton = AutoFindElement("ApplyIdPage_SendApplicationButton");
		//	sendApplicationButton.Click();

		//	AppiumElement confirmSendButton = AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "button1"));
		//	confirmSendButton.Click();

		//	AppiumElement passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
		//	passwordEntry.Click();
		//	//Now write in the real password
		//	actions.SendKeys("135135135135135").Perform();

		//	AppiumElement enterPassword = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
		//	enterPassword.Click();
		//}
		#endregion
	}
}
