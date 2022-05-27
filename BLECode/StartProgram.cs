using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BLECode
{
    class StartProgram
    {
        // Class Fields & Constants
        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredDeviceInformation = new ConcurrentDictionary<String, DeviceInformation>();

        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredXsensDot = new ConcurrentDictionary<String, DeviceInformation>();

        private static ConcurrentDictionary<String, BluetoothLEDevice> _connectedBLEDevices = new ConcurrentDictionary<string, BluetoothLEDevice>();

        private const string SensorNameStr = "Xsens DOT";
        private const int NumSensorsExpected = 1;


        static async Task Main(string[] args)
        {
            // Introductory Stuff
            Console.WriteLine("Running Xsens DOT Discovery Program - Assembly date 23/05/2022");
            Console.WriteLine("BLE Discovery Code Stage - Passed");
            Console.WriteLine();

            // Discover
            Console.WriteLine(">> Discovering all BLE devices nearby");

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
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;


            }).Start();

            deviceWatcher.Start();
            

            while (_uncoveredXsensDot.Count() != NumSensorsExpected) // wait until all the sensors has been found
            {
                Thread.Sleep(700);
                Console.WriteLine($"{_uncoveredXsensDot.Count()} of {NumSensorsExpected} {SensorNameStr} found");
            }

            deviceWatcher.Stop();


            // Connection
            Console.WriteLine(">> Attempting to connect to Xsens DOTs");

            foreach (KeyValuePair<string, DeviceInformation> entry in _uncoveredXsensDot)
            {
                BluetoothLEDevice tempBluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(entry.Value.Id);

                if (tempBluetoothLeDevice != null)
                {
                    _connectedBLEDevices.TryAdd(entry.Key, tempBluetoothLeDevice);
                    Console.WriteLine($">> Successful connection to {entry.Key}");
                }
                else
                {
                    Console.WriteLine($">> Connection to {entry.Key} failed");

                }
            }

            // Listing the services
            Console.WriteLine(">> Listing Services");

            Console.WriteLine(">>>>>>> STEP 1");
            BluetoothLEDevice bluetoothLeDevice = _connectedBLEDevices.Values.ElementAt(0);

            Console.WriteLine(">>>>>>> STEP 2");

            GattDeviceServicesResult serviceResult = await bluetoothLeDevice.GetGattServicesAsync();


            if (serviceResult.Status == GattCommunicationStatus.Success)
            {
                Console.WriteLine("Service is a success");

                var services = serviceResult.Services;
                foreach (var service in services)
                {
                    Console.WriteLine($"UUID: {service.Uuid}");
                }

            }
            else
            {
                Console.WriteLine("Service listing has failed");
            }

            // We are stalling until we get our list

            Console.WriteLine(">>>>>>> STEP 3");

            while (serviceResult.Status != GattCommunicationStatus.Success)
            {
                Console.WriteLine(">>>>>>> STEP 4");

                while (serviceResult.Services.Count < 1)
                {
                    Console.WriteLine(">>>>>>> STEP 5");

                    Console.WriteLine("Grabbing the list");
                    Thread.Sleep(300);
                }
            }

            Console.WriteLine(">>>>>>> STEP 6");

            while (serviceResult.Status != GattCommunicationStatus.Success)
            {
                Console.WriteLine("I'm success");
                if (serviceResult.Status == GattCommunicationStatus.Success)
                {
                    var services = serviceResult.Services;
                    foreach (var service in services)
                    {
                        Console.WriteLine($"UUID: {service.Uuid}");
                        Console.WriteLine(">>>>>>> STEP 7");
                        
                        if (service.Uuid.ToString().Equals("15173000-4947-11e9-8646-d663bd873d93")) // only works with the 7f DOT
                        {
                            Console.Write(" <- Battery Service");
                            //_batteryService = service; // saving the service for later use
                        }
                        Console.WriteLine();
                        
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(">> Listing Services Failed");
                }
            }

            


            // Start the watcher
            //deviceWatcher.Start();

            // List the Xsens found


            // Endline
            Console.WriteLine("Press any to exit");
            Console.ReadKey();
        }

        private static void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Console.WriteLine(">> BLE Watcher Stopped");
            Console.WriteLine();
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
