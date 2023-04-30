using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinEssentials = Xamarin.Essentials;

namespace Ble.Client
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtServPage : ContentPage
    {
        private readonly IDevice _connectedDevice;                              // Pointer for the connected BLE Device
        private readonly List<IService> _servicesList = new List<IService>();   // List for the available Services on the BLE Device

        public BtServPage(IDevice connectedDevice)                              // constructor (the function that is called when an instance of a class is defined)
        {
            InitializeComponent();

            _connectedDevice = connectedDevice;                                 // The BtServPage is called after the user has selected a BLE device. The handle to the BLE device is passed when navigating to this page through the connectedDevice function. We need to store connectedDevice into a local variable so that it can be used as long as this page is active. For that we have defined the local variable _connectedDevice. 
            bleDevice.Text = "Selected BLE device: " + _connectedDevice.Name;   // Write to the GUI the name of the BLE device we are connected to
        }

        protected async override void OnAppearing()                             // When the page is called we would like to see the services available. The services can only be incquired with an asynchronous call as it takes time for the Bluetooth adapter to reply. This is not possible in the constructor as a constructor is per definition synchronous. Therefore, we need to override the OnAppearing function so that we can inquire the available services straightaway when the page is loaded.
        {
            base.OnAppearing();

            try
            {
                var servicesListReadOnly = await _connectedDevice.GetServicesAsync();           // Read in the Services available

                _servicesList.Clear();
                var servicesListStr = new List<String>();
                for(int i = 0; i < servicesListReadOnly.Count; i++)                             // Cycle through the found interfaces
                {
                    _servicesList.Add(servicesListReadOnly[i]);                                 // Write to a list of service interfaces
                    servicesListStr.Add(servicesListReadOnly[i].Name + ", UUID: " + servicesListReadOnly[i].Id.ToString());                         // Write the name of the services seperately to an array of strings that can be used to populate the list in the GUI
                }
                foundBleServs.ItemsSource = servicesListStr;                                   // Write the found names to the list in the GUI
            }
            catch
            {
                await DisplayAlert("Error initializing", $"Error initializing UART GATT service.", "OK");
            }
        }

        private async void FoundBleServs_ItemTapped(object sender, ItemTappedEventArgs e)       // Function that is called when someone selects a Service interface to see the Characteristics of that interface
        {
            var selectedService = _servicesList[e.ItemIndex];
            if (selectedService != null)                                                        // Make sure the selected Service (still) exists
            {   
                await Navigation.PushAsync(new BtCharPage(_connectedDevice, selectedService));  // Navigate to the Characteristics site
            }
        }
    }
}