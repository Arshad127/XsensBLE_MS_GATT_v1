using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Media.Playback;

namespace BLECode
{
    class StartProgram
    {
        // Class Fields & Constants
        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredDeviceInformation = new ConcurrentDictionary<String, DeviceInformation>();

        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredXsensDot = new ConcurrentDictionary<String, DeviceInformation>();

        private static ConcurrentDictionary<String, BluetoothLEDevice> _connectedBLEDevices = new ConcurrentDictionary<string, BluetoothLEDevice>();

        private const string SensorNameStr = "Xsens DOT";
        private const int NumSensorsExpected = 2;


        static async Task Main(string[] args)
        {
            // Introductory Stuff

            Printer.Magenta("Running Xsens DOT Discovery Program - Assembly date 23/05/2022");
            Printer.Magenta("Working as a GATT Client");
            Printer.Magenta("BLE Discovery Code Stage - Passed");
            Console.WriteLine();

            // Discover
            Printer.Cyan(">> Discovering all BLE devices nearby");

                // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                                                            requestedProperties,
                                                                            DeviceInformationKind.AssociationEndpoint);

                // Register event handlers before starting the watcher.
                // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

                // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Discovery happening
            //new Thread(() =>
            //{
            //    Thread.CurrentThread.IsBackground = true;

            //}).Start();

            deviceWatcher.Start();
            

            while (_uncoveredXsensDot.Count() != NumSensorsExpected) // wait until all the sensors has been found
            {
                Thread.Sleep(500);

                if (_uncoveredXsensDot.Count() == NumSensorsExpected)
                {
                    Printer.Green($"{_uncoveredXsensDot.Count()} of {NumSensorsExpected} {SensorNameStr} found");
                }
                else
                {
                    Printer.Red($"{_uncoveredXsensDot.Count()} of {NumSensorsExpected} {SensorNameStr} found");
                }
            }

            deviceWatcher.Stop();


            // Connection
            Printer.Cyan(">> Attempting to connect to Xsens DOTs");

            foreach (KeyValuePair<string, DeviceInformation> entry in _uncoveredXsensDot)
            {
                BluetoothLEDevice tempBluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(entry.Value.Id);

                if (tempBluetoothLeDevice != null)
                {
                    _connectedBLEDevices.TryAdd(entry.Key, tempBluetoothLeDevice);
                    Printer.Green($">> Successful connection to {entry.Key}");
                }
                else
                {
                    Printer.Red($">> Connection to {entry.Key} failed");
                }
            }

            // Confirmation that the connection to the sensors is successful
            if (_connectedBLEDevices.Count() == NumSensorsExpected)
            {
                Printer.Green($">>> Connection to {NumSensorsExpected} has been successful");
            }
            else
            {
                Printer.Red($">> Failed to connect to {NumSensorsExpected - _connectedBLEDevices.Count()} IMUs");
            }

            // Listing the services
            Printer.Cyan(">> Listing All Services Available from all IMUs");
            var count = 1;
            foreach (KeyValuePair<string, BluetoothLEDevice> entry in _connectedBLEDevices)
            {
                Printer.Magenta($">>> Services for IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");

                GattDeviceServicesResult tempServiceResult = await entry.Value.GetGattServicesAsync();

                if (tempServiceResult.Status == GattCommunicationStatus.Success)
                {
                    var services = tempServiceResult.Services;
                    foreach (var service in services)
                    {
                        Printer.White($"    UUID: {service.Uuid}");
                    }
                }
                else
                {
                    Printer.Red("    Service listing has failed");
                }

                Console.WriteLine();
                count++;
            }

            // Listing the battery details
            Printer.Cyan(">> Listing the battery status of the IMUs");
            count = 1;
            foreach (KeyValuePair<string, BluetoothLEDevice> entry in _connectedBLEDevices)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");

                GattDeviceServicesResult tempServiceResult = await entry.Value.GetGattServicesAsync();

                if (tempServiceResult.Status == GattCommunicationStatus.Success)
                {
                    var services = tempServiceResult.Services;
                    foreach (var service in services)
                    {
                        if (service.Uuid.ToString().Equals("15173000-4947-11e9-8646-d663bd873d93")) // only works with the 7f DOT
                        {
                            try
                            {
                                Printer.Blue($"    UUID: {service.Uuid} <- Battery Service");
                                GattCharacteristicsResult characteristicResult = await service.GetCharacteristicsAsync();

                                if (characteristicResult.Status == GattCommunicationStatus.Success)
                                {
                                    var characteristics = characteristicResult.Characteristics;
                                    Console.WriteLine("List of characteristics associate with the battery service");
                                    foreach (var characteristic in characteristics)
                                    {
                                        GattReadResult result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                                        if (result.Status == GattCommunicationStatus.Success)
                                        {
                                            // Extract fields :
                                            var batteryLevel = result.Value.ToArray()[0].ToString();
                                            var chargingStatus = result.Value.ToArray()[1].ToString();

                                            Console.WriteLine($">>> Battery Lv: {batteryLevel}% : ChargingStatus: {chargingStatus}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"NOPE");
                                        }

                                    }
                                }
                                else
                                {
                                    Printer.Red("    Characteristic result wasn't a success");
                                }

                            }
                            catch (Exception e)
                            {
                                //Printer.Red($"     > Exception in listing: {e.Message}");
                                Thread.Sleep(300);
                            }
                        }
                    }
                }
                else
                {
                    Printer.Red("    Service listing has failed");
                }
                count++;
            }


            // Discovery happening
            //new Thread(() =>
            //{
            //    Thread.CurrentThread.IsBackground = true;

            //}).Start();




            // Start the watcher
            //deviceWatcher.Start();

            // List the Xsens found


            // Endline
            Console.WriteLine("Press any to exit");
            Console.ReadKey();
        }

        private static void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            //Console.WriteLine(">> BLE Watcher Stopped");
            //Console.WriteLine();
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Console.WriteLine(">> BLE Enumeration Completed");
            Console.WriteLine();
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (!_uncoveredDeviceInformation.ContainsKey(args.Id))
            {
                _uncoveredDeviceInformation.TryAdd(args.Id, args);

                Console.Write($"-> Name: {args.Name}, ID: {args.Id}, IsEnabled: {args.IsEnabled}");

                if (args.Name.Equals(SensorNameStr))
                {
                    _uncoveredXsensDot.TryAdd(args.Id, args);
                    Console.WriteLine("<-- Xsens DOT");
                }
                else
                {
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("What are we doing here");
            }

            


            //throw new NotImplementedException();
        }

    }
}
