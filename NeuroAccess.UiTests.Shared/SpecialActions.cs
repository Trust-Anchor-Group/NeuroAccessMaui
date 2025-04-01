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
			this.ActionBuilder.AddActions(new PointerDownInteraction1(this.GetPointer()));
			return this;
		}
		public SpecialActions Release() {
			this.ActionBuilder.AddActions(new PointerUpInteraction1(this.GetPointer()));
			return this;
		}
		public SpecialActions MoveByOffset(int xOffset, int yOffset, int durationInMS, CoordinateOrigin origin = CoordinateOrigin.Pointer) {
			this.ActionBuilder.AddAction(new PointerMoveInteraction1(this.GetPointer(), origin, xOffset, yOffset, TimeSpan.FromMilliseconds(durationInMS), new PointerEventProperties()));
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
					this.Pointer = new PointerInputDevice(PointerKind.Mouse, "special actions mouse");
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
		private class PointerUpInteraction1 : Interaction {
			private MouseButton button;

			private PointerEventProperties eventProperties;

			public PointerUpInteraction1(InputDevice sourceDevice, MouseButton button = MouseButton.Touch, PointerEventProperties? properties = null)
				 : base(sourceDevice) {
				this.button = button;
				if (properties != null) {
					this.eventProperties = properties;
				} else {
					this.eventProperties = new PointerEventProperties();
				}
			}

			public override Dictionary<string, object> ToDictionary() {
				Dictionary<string, object> dictionary = ((eventProperties != null) ? eventProperties.ToDictionary() : new Dictionary<string, object>());
				dictionary["type"] = "pointerUp";
				dictionary["button"] = Convert.ToInt32(button, CultureInfo.InvariantCulture);
				return dictionary;
			}

			public override string ToString() {
				return "Pointer up";
			}
		}
		private class PointerDownInteraction1 : Interaction {
			private MouseButton button;

			private PointerEventProperties eventProperties;

			public PointerDownInteraction1(InputDevice sourceDevice, MouseButton button = MouseButton.Touch, PointerEventProperties? properties = null)
				 : base(sourceDevice) {
				this.button = button;
				if(properties != null) {
					this.eventProperties = properties;
				} else {
					this.eventProperties = new PointerEventProperties();
				}
			}

			public override Dictionary<string, object> ToDictionary() {
				Dictionary<string, object> dictionary = ((eventProperties != null) ? eventProperties.ToDictionary() : new Dictionary<string, object>());
				dictionary["type"] = "pointerDown";
				dictionary["button"] = Convert.ToInt32(button, CultureInfo.InvariantCulture);
				return dictionary;
			}

			public override string ToString() {
				return "Pointer down";
			}
		}

		public class PointerMoveInteraction1 : Interaction {

			private int x;

			private int y;

			private TimeSpan duration = TimeSpan.MinValue;

			private CoordinateOrigin origin = CoordinateOrigin.Pointer;

			private PointerEventProperties eventProperties;

			public PointerMoveInteraction1(InputDevice sourceDevice, CoordinateOrigin origin, int x, int y, TimeSpan duration, PointerEventProperties properties)
				 : base(sourceDevice) {

				this.origin = origin;


				if (duration != TimeSpan.MinValue) {
					this.duration = duration;
				}

				this.x = x;
				this.y = y;
				eventProperties = properties;
			}

			public override Dictionary<string, object> ToDictionary() {
				Dictionary<string, object> dictionary = ((eventProperties != null) ? eventProperties.ToDictionary() : new Dictionary<string, object>());
				dictionary["type"] = "pointerMove";
				if (duration != TimeSpan.MinValue) {
					dictionary["duration"] = Convert.ToInt64(duration.TotalMilliseconds);
				}

				//if (target != null) {
				//	dictionary["origin"] = ConvertElement();
				//} else {
				dictionary["origin"] = origin.ToString().ToLowerInvariant();
				//}

				dictionary["x"] = x;
				dictionary["y"] = y;
				return dictionary;
			}

			public override string ToString() {
				string text = origin.ToString();
				//if (origin == CoordinateOrigin.Element) {
				//	text = target.ToString();
				//}

				return string.Format(CultureInfo.InvariantCulture, "Pointer move [origin: {0}, x offset: {1}, y offset: {2}, duration: {3}ms]", text, x, y, duration.TotalMilliseconds);
			}

			//	private Dictionary<string, object> ConvertElement() {
			//		IWebDriverObjectReference webDriverObjectReference = target as IWebDriverObjectReference;
			//		if (webDriverObjectReference == null && target is IWrapsElement wrapsElement) {
			//			webDriverObjectReference = wrapsElement.WrappedElement as IWebDriverObjectReference;
			//		}

			//		if (webDriverObjectReference == null) {
			//			throw new ArgumentException("Target element cannot be converted to IWebElementReference");
			//		}

			//		return webDriverObjectReference.ToDictionary();
			//	}
			//}

		}
	}
}
