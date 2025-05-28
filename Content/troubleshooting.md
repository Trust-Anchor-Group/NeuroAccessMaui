# Troubleshooting
This guide will help you get started making automated UI tests for the app.
##
- [Troubleshooting](#troubleshooting)
- [Getting started](#getting-started)
  - [prerequisites](#prerequisites)
  - [Making a page ready for testing](#making-a-page-ready-for-testing)
    - [Naming conventions](#naming-conventions)
- [Create a test Class](#create-a-test-class)
- [Create a test method](#create-a-test-method)
- [Run a test/tests](#run-a-testtests)
- [How to find an AppiumElement (view) that can be used in a test method](#how-to-find-an-appiumelement-view-that-can-be-used-in-a-test-method)
  - [AutoFindElement](#autofindelement)
  - [ScrollUpOrDownAndFindElement](#scrollupordownandfindelement)
  - [FindElementByText](#findelementbytext)
  - [FindElementByTextContaintedInText](#findelementbytextcontaintedintext)
  - [FindElementByAttribute](#findelementbyattribute)
  - [FindElementByTextContainedInAttribute](#findelementbytextcontainedinattribute)
- [How to use an AppiumElement in a test method](#how-to-use-an-appiumelement-in-a-test-method)
  - [WriteInAndTestComplexEntry](#writeinandtestcomplexentry)
  - [Testing if a view's text changed](#testing-if-a-views-text-changed)
- [Other information](#other-information)
  - [How the methods run in MSTest](#how-the-methods-run-in-mstest)
## Getting started
### prerequisites 
- Install **Node.js** because it's required for Appium.
- To install Appium open command prompt (cmd) and run: **npm install -g appium**
- Run also: **appium driver install uiautomator2**
### Where to find the test project 
Go to the branch **ui-testing**. After that click on **Solution Files** in the solution explorer, and there you will find the project **NeuroAccess.UiTests.Shared**.
### Making a page ready for testing
It's really simple to access views found in XAML pages while testing. You just need to add the attribute **AutomationId** to the view you want to access, and then give it a value. There are though naming conventions you have to think of before choosing what the AutomationId of a view should become.
#### Naming conventions
- You need to have the full name of the page in the beginning of the AutomationId for every view found in the page, and an underscore after it. Example: **"SomePage_..."**. Though the AutomationId for the page itself should not contain an underscore, but only the page name.
- After the underscore you can choose a name if you want. Make it preferably hint towards the view's function.
- After the name, or the underscore if no name was chosen, you have to write the full name of the view type. Example: **"SomePage_AddTemplatedButton"**, "Add" is the name and "TemplatedButton" is the view type in this example.

## Create a test Class
All test classes need to have **[TestClass]** on top of them. They also need to inherit the class **BaseTest**. In the class constructor when also calling the base class constructor (like this: **public SomeTestClass(): base(...)**) there is parameters the base class takes in. Here is the list of them and their explanation:

- **reset**: It's of type bool, and it's obligatory to assign a value to it. Assign false to it if you want the app to fully close and then reopen. Assign true to it if you want the app to reset and reopen.
- **className**: It's of type string, and it's not obligatory to assign a value to it. Assign the class mame to it (like this: nameof(someClassName)) if you don't want the app to reset/close and reopen before every test method in the class. Assign an empty string to it or leave it to make the app reset/close and reopen before every test method in the class.
- **skipFirstTestForceReset**: It's of type bool, and it's obligatory to assign a value to it. The first test ignores the reset parameter and a reset happens forcefully. However if you assign true to this parameter, the first test force reset won't happen and the reset variable won't be ignored. Assign false to it or leave to not skip the first test force reset.

## Create a test method
All test methods need to have **[TestMethod]** and **[TestCategory("Android")]** on top of them. Right now only testing for Android is available, which is why the test category is Android. A test in the MSTest framework succeeds if no error happens, and it fails otherwise. You can make it fail by either throwing an exception (error) normally (like this: throw new Exception("some message")) or using MSTest method Assert.Fail or other MSTest methods like Assert.AreEqual. We primarily used the normal exception throwing method to make a test fail, but you are free to use the one you want.

## Run a test/tests
Click on View which is right next to Edit, which is right next to File on the top left of Visual studio after opening the project. After that click on Test Explorer, which will show a window where the available tests are displayed. You can also run a test, all tests or some tests using this window.

## How to find an AppiumElement (view) that can be used in a test method

In this section below you will find a list of methods to find an AppiumElement (view) accessible by having the test class inherit the BaseTest class

Before that some of these methods can only be used after looking at what the AppiumElement one is trying to find contains for attributes, and the value of these attributes. To find all of this information have a temporary test send the value of App.PageSource (when the views you are trying to find information about are on the screen) using either **Console.WriteLine()** or **throw new Exception()**

### AutoFindElement
This method automatically finds an AppiumElement by its AutomationId and then returns it, all within the time it's allowed to run for. If it doesn't find the element in the time it's allowed to run for, then it throws an exception (i.e error). Here is a list of the parameters it takes in:
- **automationId**: Assign the AutomationId of the view you want to find to it.
- **maxTryTimeInS**: It's of type double, and it's not obligatory to assign a value to it. The default value of it is 20, and this parameter determines how many seconds the method is gonna run for while the view hasn't been found yet. Assign by example 5 to it, if you want the method to run for max 5 seconds.

### ScrollUpOrDownAndFindElement
This method scrolls up or down based on what you choose, and automatically finds an AppiumElement by the AutomationId you provide to it, and then returns it. If it doesn't find the element after the amount you choose of near screensizes it scrolls up or down, then it throws an exception. Here is a list of the parameters it takes in:

- **automationIDToFind**: It's of type string, and it's obligatory to assign a value to it. Assign the AutomationId of the view you want to find to it.
- **scrollUpNotDown**: It's of type bool, and it's obligatory to assign a value to it. Assign true to it if you want it to scroll up, and false if you want it to scroll down.
- **howManyScreenSizesToScroll**: It's of type int, and it's obligatory to assign a value to it. Assign a number of the amount of the almost complete vertical size of the mobile screen it's gonna scroll for.
- **specialActions**: It's of type SpecialActions, and it's not obligatory to assign a value to it. Assign a value to it if you use these methods a lot, and you want them to use the same SpecialActions class, instead of too many SpecialActions classes for no reason.
- **xPosistionToStartFrom**: It's of type int, and it's not obligatory to assign a value to it. Its default value is 10. The position of the left side of the mobile is 0, and this parameter determines how much away horizontally from the let side the scrolling will start from.
- **xPosistionToStartFrom**: It's of type int, and it's not obligatory to assign a value to it. Its default value is null which later in the method becomes half the screensize vertically. The position of the top side of the mobile is 0, and this parameter determines how much away vertically from the top side the scrolling will start from. Assign null to it or leave it if you want the scrolling to start from half the screen vertically. Assign by example 300 to it if you want the scrolling to start from there vertically.

### ScrollUpOrDownAndCustomFindElement and AutoCustomFindElement
They work the same as the ones before, but instead of finding an AppiumElement by an AutomationId, it takes in a custom method you choose that returns an AppiumElement.
Example:
- AutoCustomFindElement(() => FindElementByTextContainedInAttribute("resource-id", "button2"));

### FindElementByText
Don't use this method a lot because depending on the language the text will usually change. It takes only one parameter which is the text of the AppiumElement you are trying to find. It throws an exception if no element is found.

### FindElementByTextContaintedInText
Also don't use this method a lot because depending on the language the text will usually change. It takes only one parameter which is text contained in the text of the AppiumElement you are trying to find.  It throws an exception if no element is found.

### FindElementByAttribute
This method tries to find an AppiumElement by an attribute and its value, that should be present in the element you are trying to find. By example the attribute **content-desc** and the value **Open navigation drawer**, if the attribute exists in an element and its value is the value provided then that element is returned by this method. It throws an exception if no element is found.

### FindElementByTextContainedInAttribute
This method tries to find an AppiumElement by checking if the attribute and it's value provided to this method, is present in an AppiumElement that is currently on the screen. It throws an exception if no element is found.

## How to use an AppiumElement in a test method
First find an AppiumElement (i.e view) by a method you choose and then you can use it. One of the most usual things you will do with it is clicking it to test navigation. It's done like this: **aButton.Click();**. After clicking it, just use a method to find a view in the next page to test if the navigation was successful. Down you will also find a list of what else you can test, for example an input field(called Entry in .NetMaui).

### WriteInAndTestComplexEntry
This method writes what you want in an Entry (inputfield of .Net Maui), and at the same time tests if the Entry is working correctly. It throws an Exception if it isn't working correctly. Here is a list of the parameters it takes in:

- **entryAutomationId**: It's of type string, and it's obligatory to assign a value to it. Assign the AutomationId of the Entry you are testing to it.
- **whatToWrite**: It's of type string, and it's obligatory to assign a value to it. Assign what you want to be written in the Entry to it.
- **entry**: It's of type AppiumElement, and it's not obligatory to assign a value to it. The default value is null. Assign null to it or leave it if you don't want the Entry to be clicked (maybe then nothing gets written and the test fails). Assign the Entry to it if you want it to be clicked before text input is sent to the emulator.
- **actions**: It's of type Actions, and it's not obligatory to assign a value to it. Assign a value to it if you use these methods a lot, and you want them to use the same Actions class, instead of too many Actions classes for no reason.

### Testing if a view's text changed
Sometimes a button is pressed and then another views' text changes. To test if it actually changed, first before pressing the button get the view's text. Which you do by first finding it with a method, and then getting its text value by doing this: **string text = aView.GetAttribute("text");**. Then after pressing the button get the text value again and compare it with the earlier one.

## Other information
### How the methods run in MSTest
In MSTest for every test method in a test class a new copy of the test class is made to run the method. So to have variables that you can use for all methods, that stay the same across tests in the test class unless changed, you have to make them static. 
Also instead of the constructor which will run once for every test method, use a method with [ClassInitialize] on top of it, and make it also public and static, and give it this parameter: **TestContext context**. This method will run only once before all the tests in the test class. Example of how it can look like:

**[ClassInitialize]**

**public static void ClassInitialize(TestContext context) {**
### Usual errors that happen
#### 1) The view cannot be found even though it has an AutomationId
When adding new AutomationIds to the project, you need to run the app normally again, and then take the updated app in the files, copy it, and then replace the app with the new version in the test project. 
The detailed explanation is like this:
- Run the app and then close it.
- In the file explorer, go to this file path: **"C:\My Projects\NeuroAccessMaui\NeuroAccessMaui\bin\Debug\net8.0-android"**.
- Copy these three files at the same time: 1(**com.tag.NeuroAccess.apk**), 2(**com.tag.NeuroAccess-Signed.apk**), 3(**com.tag.NeuroAccess-Signed.apk.idsig**).
- Got to this file path: **"C:\My Projects\NeuroAccessMaui\NeuroAccess.UiTests.Android\bin\Debug\net8.0"**.
- Replace the files with the same name as the files from before, with the copy of the files of before. Also delete other files that start with: "com.tag.NeuroAccess".
- Now it should work correctly and the view should be able to be found now!

#### 2) Error happens for weird reason before the test even actually beginning
Sometimes after closing a running test midway, launching a test next time will result in an error. You can just ignore this error, and when launching again it should work just fine.

#### 3) The button isn't becoming pressable
Sometimes using the method the AppiumElement method called sendkeys, a button that should have became pressable doesn't become pressable. So Instead use this the sendkeys method from the Actions class. 
It looks like this: 

**Actions actions = new Actions(App);**

**actions.SendKeys(whatToWrite).Perform();**

Or just use the **WriteInEmulator** method found in BaseTest, that is in itself using the sendkeys method from the Actions class.










