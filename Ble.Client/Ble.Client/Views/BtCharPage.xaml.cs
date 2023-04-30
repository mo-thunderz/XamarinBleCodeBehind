using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinEssentials = Xamarin.Essentials;

namespace Ble.Client
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
                    var charListReadOnly = await _selectedService.GetCharacteristicsAsync();       // Read in available Characteristics

                    _charList.Clear();
                    var charListStr = new List<String>();
                    for (int i = 0; i < charListReadOnly.Count; i++)                               // Cycle through available interfaces
                    {
                        _charList.Add(charListReadOnly[i]);                                        // Write to a list of Chars
                        // IMPORTANT: listview cannot cope with entries that have the exact same name. That is why I added "i" to the beginning of the name. If you add the UUID you can delete "i" again.
                        charListStr.Add(i.ToString() + ": " + charListReadOnly[i].Name);           // Write to a list of Strings for the GUI
                    }
                    foundBleChars.ItemsSource = charListStr;                                       // Write found Chars to the GUI
                }
                else
                {
                    ErrorLabel.Text = GetTimeNow() + "UART GATT service not found." + Environment.NewLine;
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void FoundBleChars_ItemTapped(object sender, ItemTappedEventArgs e)       // This function is run when a Characteristic is selected
        {

            _char = _charList[e.ItemIndex];                                                     // select Char
            if (_char != null)                                                                  // make sure Char exists
            {
                bleChar.Text = _char.Name + "\n" +                                              // write information on Char to GUI
                    "UUID: " + _char.Uuid.ToString() + "\n" +
                    "Read: " + _char.CanRead + "\n" +                                           // indicates whether characteristic can be read from
                    "Write: " + _char.CanWrite + "\n" +                                         // indicates whether characteristic can be written to
                    "Update: " + _char.CanUpdate;                                               // indicates whether characteristics can be updated (supports notify)

                var charDescriptors = await _char.GetDescriptorsAsync();                        // get information of Descriptors defined

                bleChar.Text += "\nDescriptors (" + charDescriptors.Count + "): ";              // write Descriptor info to the GUI
                for (int i = 0; i < charDescriptors.Count; i++)
                    bleChar.Text += charDescriptors[i].Name + ", "; 
            }
        }

        private async void RegisterCommandButton_Clicked(object sender, EventArgs e)                    // function that is run when the "Register" button is selected. This is for Characteristics that support "Notify". A Callback function will be defined that will be triggered if the selected BLE device sends information to the phone.
        {
            try
            {
                if (_char != null)                                                                      // make sure the characteristic exists
                {
                    // NOTE: in the youtube video I did not check whether or not the characteristic can be updated -> I added this afterwards
                    if (_char.CanUpdate)                                                                // check if characteristic supports notify
                    {
                        _char.ValueUpdated += (o, args) =>                                              // define a callback function
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
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not have a notify function.";
                    }
                }
                else
                {
                    ErrorLabel.Text = GetTimeNow() + ": No characteristic selected.";
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void ReceiveCommandButton_Clicked(object sender, EventArgs e)                 // This function is run when the "Receive" button is selected
        {
            try
            {
                if (_char != null)                                                                  // make sure a Characteristic is selected
                {
                    // NOTE: in the youtube video I did not check whether or not the characteristic can be read from -> I added this afterwards
                    if (_char.CanRead)                                                              // check if characteristic supports read
                    {
                        var receivedBytes = await _char.ReadAsync();                                                            // Receive value from Characteristic
                        Output.Text += Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length) + Environment.NewLine;   // Write to GUI -> NOTE: in this example the received bytes are interpretted as ASCII. Feel free to use other interpretations similar to the RegisterCommandButton_Clicked function
                    }
                    else
                    {
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not support read.";
                    }
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
                    // NOTE: in the youtube video I did not check whether or not the characteristic can be written to -> I added this afterwards
                    if (_char.CanWrite)                                                             // check if characteristic supports write
                    {
                        byte[] array = Encoding.UTF8.GetBytes(CommandTxt.Text);                     // Write CommandTxt.Text String to byte array in preparation of sending it over -> NOTE: the string is sent over as ASCII characters, feel free to use different coding
                        await _char.WriteAsync(array);                                              // Send to BLE Device
                    }
                    else
                    {
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not support Write";
                    }
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