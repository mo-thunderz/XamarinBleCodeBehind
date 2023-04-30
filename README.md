# Xamarin Ble Explorer (Client)
This is a simple BLE Explorer for Xamarin, similar to the nRF app. It allows the user to select a BLE device, then shows all available services. Upon selection of a service it will show the available Characteristics. One can then read/write strings or register to Characteristics with "notify" enabled.

Code was originally based on Jenx Bluetooth example, but then updated and changed to be compatible with modern Android phones:
https://www.jenx.si/2020/08/13/bluetooth-low-energy-uart-service-with-xamarin-forms/

Code is written for Android 31 or newer
All logic is placed in Code Behind in this example.
