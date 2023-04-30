using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Xamarin.Essentials.Permissions;
using XamarinEssentials = Xamarin.Essentials;

namespace Ble.Client
{
    [DesignTimeVisible(false)]
    public partial class BtDevPage : ContentPage
    {
        private readonly IAdapter _bluetoothAdapter;                            // Class for the Bluetooth adapter
        private readonly List<IDevice> _gattDevices = new List<IDevice>();      // Empty list to store BLE devices that can be detected by the Bluetooth adapter

        public BtDevPage()                                                      // constructor (the function that is called when an instance of a class is defined)
        {
            InitializeComponent();

            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;               // Point _bluetoothAdapter to the current adapter on the phone
            _bluetoothAdapter.DeviceDiscovered += (sender, foundBleDevice) =>   // When a BLE Device is found, run the small function below to add it to our list
            {
                if (foundBleDevice.Device != null && !string.IsNullOrEmpty(foundBleDevice.Device.Name))
                    _gattDevices.Add(foundBleDevice.Device);
            };
        }

        private async Task<bool> PermissionsGrantedAsync()      // Function to make sure that all the appropriate approvals are in place
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return status == PermissionStatus.Granted;
        }

        private async void ScanButton_Clicked(object sender, EventArgs e)           // Function that is called when the scanButton is pressed
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = false);        // Swith the Isbusy Indicator on
            foundBleDevicesListView.ItemsSource = null;                                                     // Empty the list of found BLE devices (in the GUI)

            if (!await PermissionsGrantedAsync())                                                           // Make sure there is permission to use Bluetooth
            {
                await DisplayAlert("Permission required", "Application needs location permission", "OK");
                IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);
                return;
            }

            _gattDevices.Clear();                                                                           // Also clear the _gattDevices list

            if (!_bluetoothAdapter.IsScanning)                                                              // Make sure that the Bluetooth adapter is scanning for devices
            {
                await _bluetoothAdapter.StartScanningForDevicesAsync();
            }

            foreach (var device in _bluetoothAdapter.ConnectedDevices)                                      // Make sure BLE devices are added to the _gattDevices list
                _gattDevices.Add(device);
            
            foundBleDevicesListView.ItemsSource = _gattDevices.ToArray();                                   // Write found BLE devices to GUI
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);         // Switch off the busy indicator
        }

        private async void FoundBluetoothDevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)   // Function that is run whenever a detected BLE device is selected
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = false);        // Switch on IsBusy indicator
            IDevice selectedItem = e.Item as IDevice;                                                       // The item selected is an IDevice (detected BLE device). Therefore we have to cast the selected item to an IDevice

            if (selectedItem.State == DeviceState.Connected)                                                // Check first if we are already connected to the BLE Device 
            {
                await Navigation.PushAsync(new BtServPage(selectedItem));                                   // Navigate to the Services Page to show the services of the selected BLE Device
            }
            else
            {
                try
                {
                    var connectParameters = new ConnectParameters(false, true);                             
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedItem, connectParameters);          // if we are not connected, then try to connect to the BLE Device selected
                    await Navigation.PushAsync(new BtServPage(selectedItem));                               // Navigate to the Services Page to show the services of the selected BLE Device
                }
                catch
                {
                    await DisplayAlert("Error connecting", $"Error connecting to BLE device: {selectedItem.Name ?? "N/A"}", "Retry");       // give an error message if it is not possible to connect
                }
            }

            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);         // switch off the "Isbusy" indicator
        }
    }
}