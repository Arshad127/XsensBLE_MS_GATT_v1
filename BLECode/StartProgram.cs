using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BLECode
{
    class StartProgram
    {
        // Class Fields & Constants
        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredDeviceInformation = new ConcurrentDictionary<String, DeviceInformation>();

        private static ConcurrentDictionary<String, DeviceInformation> _uncoveredXsensDot = new ConcurrentDictionary<String, DeviceInformation>();

        private const string SearchStr = "Xsens DOT";


        static void Main(string[] args)
        {
            // Introductory Stuff
            Console.WriteLine("Running Xsens DOT Discovery Program - Assembly date 23/05/2022");
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

                // Start the watcher
            deviceWatcher.Start();

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

                if (args.Name.Equals(SearchStr))
                {
                    _uncoveredXsensDot.TryAdd(args.Id, args);
                    Console.WriteLine("<-- Xsens Dot");
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
