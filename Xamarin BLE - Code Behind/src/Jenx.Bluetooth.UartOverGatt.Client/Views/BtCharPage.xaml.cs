using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinEssentials = Xamarin.Essentials;

namespace Jenx.Bluetooth.UartOverGatt.Client
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtCharPage : ContentPage
    {
        private readonly IDevice _connectedDevice;                                      // Pointer for the connected BLE Device
        private readonly IService _selectedService;                                     // Pointer to the selected service
        private readonly List<ICharacteristic> _charList = new List<ICharacteristic>(); // List for the available Characteristics on the BLE Device
        private ICharacteristic _char;                                                  // Pointer to the selected characteristic

        public BtCharPage(IDevice connectedDevice, IService selectedService)            // constructor (the function that is called when an instance of a class is defined)
        {
            InitializeComponent();

            _connectedDevice = connectedDevice;                                         // When the BtCharPage is called, a user has selected a BLE device (connectedDevice) and a service (selectedService). These parameters must be stored in this class for later use
            _selectedService = selectedService;
            _char = null;                                                               // When the site is initialized, no Characteristic is selected yet.

            bleDevice.Text = "Selected BLE device: " + _connectedDevice.Name;           // Write the selected BLE Device and Service to the GUI
            bleService.Text = "Selected BLE service: " + _selectedService.Name;
        }

        protected async override void OnAppearing()                                     // When the page is called we would like to see the chars available. The chars can only be incquired with an asynchronous call as it takes time for the Bluetooth adapter to reply. This is not possible in the constructor as a constructor is per definition synchronous. Therefore, we need to override the OnAppearing function so that we can inquire the available chars straightaway when the page is loaded.
        {
            base.OnAppearing();
            try
            {
                if (_selectedService != null)
                {
                    var chars = await _selectedService.GetCharacteristicsAsync();       // Read in available Characteristics

                    _charList.Clear();
                    var chars_list = new List<String>();
                    for (int i = 0; i < chars.Count; i++)                               // Cycle through available interfaces
                    {
                        _charList.Add(chars[i]);                                        // Write to a list of Chars
                        chars_list.Add(chars[i].Name);                                  // Write to a list of Strings for the GUI
                    }
                    foundBleChars.ItemsSource = chars_list;                             // Write found Chars to the GUI
                }
                /*
                if (_selectedService != null)
                {
                    var char1 = await _selectedService.GetCharacteristicAsync(Guid.Parse("64af9d82-a92e-479c-85cd-c0775fb55ed9"));

                    if (char1 != null)
                    {
                        var descriptors = await char1.GetDescriptorsAsync();

                        char1.ValueUpdated += (o, args) =>
                        {
                            var receivedBytes = args.Characteristic.Value;
                            Console.WriteLine("byte array: " + BitConverter.ToString(receivedBytes));

                            int char_val = 0;
                            for (int i = 0; i < receivedBytes.Length; i++)
                            {
                                char_val = char_val | (receivedBytes[i] << i * 8);
                            }

                            XamarinEssentials.MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Output.Text += char_val.ToString(); // BitConverter.ToString(receivedBytes); //  Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length) + Environment.NewLine;
                            });
                        };

                        if (char1.CanUpdate)
                        {
                            await char1.StartUpdatesAsync();
                        }
                        else
                        {
                            Debug.WriteLine("Updates not supported");
                        }
                    }
                }
                                */
                else
                {
                    Output.Text += "UART GATT service not found." + Environment.NewLine;
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void FoundBleChars_ItemTapped(object sender, ItemTappedEventArgs e)       // This function is run when a Characteristic is selected
        {
            if (_selectedService != null)                                                       // make sure Servie exists
            {
                _char = _charList[e.ItemIndex];                                                 // select Char
                bleChar.Text = _char.Name + "\n" +                                              // write information on Char to GUI
                    "UUID: " + _char.Uuid.ToString() + "\n" +
                    "Read: " + _char.CanRead + "\n" +
                    "Write: " + _char.CanRead + "\n" +
                    "Update: " + _char.CanUpdate;

                var charDescriptors = await _char.GetDescriptorsAsync();                        // get information of Descriptors defined

                bleChar.Text += "\nDescriptors (" + charDescriptors.Count + "): ";              // write Descriptor info to the GUI
                for (int i = 0; i < charDescriptors.Count; i++)
                    bleChar.Text += charDescriptors[i].Name + ", "; 
            }
        }

        private async void RegisterCommandButton_Clicked(object sender, EventArgs e)                  // function that is run when the "Register" button is selected. This is for Characteristics that support "Notify". A Callback function will be defined that will be triggered if the selected BLE device sends information to the phone.
        {
            try
            {
                if (_char != null)                                                              // make sure the characteristic exists
                {
                    _char.ValueUpdated += (o, args) =>                                          // define a callback function
                    {
                        var receivedBytes = args.Characteristic.Value;                              // read in received bytes
                        Console.WriteLine("byte array: " + BitConverter.ToString(receivedBytes));   // write to the console for debugging

                        
                        string _charStr = "";                                                                           // in the following section the received bytes will be displayed in different ways (you can select the method you need)
                        if (receivedBytes != null)
                        {
                            _charStr = "Bytes: " + BitConverter.ToString(receivedBytes);                                // by directly converting the bytes to strings we see the bytes themselves as they are received
                            _charStr += " | UTF8: " + Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length);  // This code interprets the bytes received as ASCII characters
                        }

                        if (receivedBytes.Length <= 4)
                        {                                                                                               // If only 4 or less bytes were received than it could be that an INT was sent. The code here combines the 4 bytes back to an INT
                            int char_val = 0;
                            for (int i = 0; i < receivedBytes.Length; i++)
                            {
                                char_val |= (receivedBytes[i] << i * 8);
                            }
                            _charStr += " | int: " + char_val.ToString();
                        }
                        _charStr += Environment.NewLine;                                                                // the NewLine command is added to go to the next line

                        XamarinEssentials.MainThread.BeginInvokeOnMainThread(() =>                                      // as this is a callback function, the "MainThread" needs to be invoked to update the GUI
                        {
                            Output.Text += _charStr;
                        });
                        
                    };
                    await _char.StartUpdatesAsync();

                    ErrorLabel.Text = GetTimeNow() + ": Notify callback function registered successfully.";
                }
                else
                {
                    ErrorLabel.Text = GetTimeNow() + ": UART GATT service not found.";
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void ReceiveCommandButton_Clicked(object sender, EventArgs e)         // This function is run when the "Receive" button is selected
        {
            try
            {
                if (_char != null)          // make sure a Characteristic is selected
                {
                    var receivedBytes = await _char.ReadAsync();                                                            // Receive value from Characteristic
                    Output.Text += Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length) + Environment.NewLine;   // Write to GUI -> NOTE: in this example the received bytes are interpretted as ASCII. Feel free to use other interpretations similar to the RegisterCommandButton_Clicked function
                }
                else
                    ErrorLabel.Text = GetTimeNow() + ": No Characteristic selected.";
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error receiving Characteristic.";
            }
        }
        private async void SendCommandButton_Clicked(object sender, EventArgs e)                    // This function is called when the "Send" button is selected
        {
            try
            {
                if (_char != null)                                                                  // Make sure a Characteristic is defined
                {
                    byte[] array = Encoding.UTF8.GetBytes(CommandTxt.Text);                         // Write CommandTxt.Text String to byte array in preparation of sending it over -> NOTE: the string is sent over as ASCII characters, feel free to use different coding
                    await _char.WriteAsync(array);                                                  // Send to BLE Device
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error receiving Characteristic.";
            }
        }

        private string GetTimeNow()
        {
            var timestamp = DateTime.Now;
            return timestamp.Hour.ToString() + ":" + timestamp.Minute.ToString() + ":" + timestamp.Second.ToString();
        }
    }
}