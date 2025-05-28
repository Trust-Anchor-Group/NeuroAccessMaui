using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using static OpenQA.Selenium.BiDi.Modules.Script.RemoteValue.WindowProxy;
using static OpenQA.Selenium.Interactions.PointerInputDevice;
using OpenQA.Selenium.Internal;



namespace NeuroAccess.UiTests {
	public class SpecialActions {
		public PointerInputDevice? Pointer;
		public ActionBuilder ActionBuilder = new ActionBuilder();
		public IActionExecutor ActionExecutor;

		public SpecialActions ClickAndHold() {
			this.ActionBuilder.AddActions(this.GetPointer().CreatePointerDown(MouseButton.Touch));
			return this;
		}
		public SpecialActions Release() {
			this.ActionBuilder.AddActions(this.GetPointer().CreatePointerUp(MouseButton.Touch));
			return this;
		}
		public SpecialActions MoveByOffset(int xOffset, int yOffset, int durationInMS, CoordinateOrigin origin = CoordinateOrigin.Pointer) {
			this.ActionBuilder.AddAction(this.GetPointer().CreatePointerMove(origin, xOffset, yOffset, TimeSpan.FromMilliseconds(durationInMS)));
			return this;
		}
		public SpecialActions Perform() {
			this.ActionExecutor.PerformActions(this.ActionBuilder.ToActionSequenceList());
			this.ActionBuilder.ClearSequences();
			return this;
		}
		public void forceClearActions() {
			
		}
		public SpecialActions(IWebDriver driver) {
			IActionExecutor driverAs = this.GetDriverAsIActionExecutor(driver);
			if (driverAs == null) {
				throw new ArgumentException("The IWebDriver object must implement or wrap a driver that implements IActionExecutor.", "driver");
			}
			this.ActionExecutor = driverAs;
		}
		public PointerInputDevice GetPointer() {
			if (this.Pointer == null) {
				string pointerName = "special actions mouse";
				PointerInputDevice? inputDevice = this.FindDeviceById(pointerName) as PointerInputDevice;
				if (inputDevice != null) {
					if (!(inputDevice is PointerInputDevice)) {
						throw new InvalidOperationException($"Device under the name \"{pointerName}\" is not a pointer. Actual input type: {inputDevice.DeviceKind}");
					} else {
						this.Pointer = inputDevice;
						return inputDevice;
					}
				} else {
					this.Pointer = new PointerInputDevice(PointerKind.Touch, "special actions touch");
					return this.Pointer;
				}
			}
			return this.Pointer;
		}
		//private InputDevice FindDeviceById(string name) {
		//	foreach (ActionSequence item in actionBuilder.ToActionSequenceList()) {
		//		if ((string)item.ToDictionary()["id"] == name) {
		//			return item.inputDevice;
		//		}
		//	}

		//	return null;
		//}

		public IActionExecutor GetDriverAsIActionExecutor(IWebDriver driver) {
			IActionExecutor val = driver as IActionExecutor;
			if (val == null) {
				for (IWrapsDriver wrapsDriver = driver as IWrapsDriver; wrapsDriver != null; wrapsDriver = wrapsDriver.WrappedDriver as IWrapsDriver) {
					val = wrapsDriver.WrappedDriver as IActionExecutor;
					if (val != null) {
						driver = wrapsDriver.WrappedDriver;
						break;
					}
				}
			}

			return val;
		}
		private InputDevice FindDeviceById(string name) {
			foreach (ActionSequence item in this.ActionBuilder.ToActionSequenceList()) {
				if ((string)item.ToDictionary()["id"] == name) {
					return item.InputDevice;
				}
			}

			return null;
		}

	}
}
