using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Testing
{
    
    class Program
    {
        // CLASS FIELDS
        private static DeviceInformation _device = null;
        private static Dictionary<String, DeviceInformation> _deviceList = new Dictionary<String, DeviceInformation>();

        private static GattDeviceService _batteryService;
        private static GattCharacteristic _batteryCharacteristic;

        // CLASS CONSTANTS
        private const string SearchStr = "Xsens DOT";

        static async Task Mais(string[] args)
        {
            // ----------------------------------------------------------
            //Console.WriteLine("Welcome to the BLE Test Connector! ");
            //Console.WriteLine("Initial Build Date : 15/05/2022 ");
            //Console.WriteLine("Latest Modification: 19/05/2022 ");
            // ----------------------------------------------------------

            // Find the DOT we want to connect to
            QueryDOT();
            Console.WriteLine($"Device {_device.Name} with ID {_device.Id} has been found");

            // Connect to the DOT
            Console.WriteLine("Attempting Connection / Pairing with device");
            BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(_device.Id);
            Console.WriteLine("Connection / Paring Successful");

            // Listing the Services
            Console.WriteLine(">> Listing Services");
            GattDeviceServicesResult serviceResult = await bluetoothLeDevice.GetGattServicesAsync();

            while (serviceResult.Status != GattCommunicationStatus.Success)
            {
                if (serviceResult.Status == GattCommunicationStatus.Success)
                {
                    var services = serviceResult.Services;
                    foreach (var service in services)
                    {
                        Console.Write($"UUID: {service.Uuid}");
                        if (service.Uuid.ToString().Equals("15173000-4947-11e9-8646-d663bd873d93")) // only works with the 7f DOT
                        {
                             Console.Write(" <- Battery Service");
                            _batteryService = service; // saving the service for later use
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                }
            }





            // Listing the characteristics of battery service
            Console.WriteLine("Listing Characteristics");

            GattCharacteristicsResult characteristicResult = await _batteryService.GetCharacteristicsAsync();

            if (characteristicResult.Status == GattCommunicationStatus.Success)
            {
                var characteristics = characteristicResult.Characteristics;
                Console.WriteLine("List of characteristics associate with the battery service");
                foreach (var characteristic in characteristics)
                {
                    Console.WriteLine($">>> UUID: {characteristic.Uuid}");

                    _batteryCharacteristic = characteristic;

                }
            }
            Console.WriteLine();


            // Perform subscription
            Console.WriteLine(">> Subscribing");
            GattCharacteristicProperties properties = _batteryCharacteristic.CharacteristicProperties;

            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                Console.WriteLine("Can be notified");
            }

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                Console.WriteLine("Can be read");

                // Read characteristic values :
                GattReadResult result = await _batteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    // Extract fields :
                    String batteryLevel = result.Value.ToArray()[0].ToString();
                    String chargingStatus = result.Value.ToArray()[1].ToString();

                    Console.WriteLine($">>> Battery Lv: {batteryLevel}% : ChargingStatus: {chargingStatus}");
                }
                else
                {
                    Console.WriteLine($"NOPE");
                }

                
            }

            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                Console.WriteLine("Can be written to");
            }








            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }



        private static void QueryDOT()
        {
            // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
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

            while (_device == null)
            {
                //Thread.Sleep(200);
                Console.WriteLine($"Searching for {SearchStr} products in the vicinity....");
            }

            deviceWatcher.Stop();
        }

        private static void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
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
            //Console.WriteLine(args.Name);
            if(args.Id.ToString().Equals("BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f"))
                //if (args.Name.Equals(SearchStr))
            {
                _device = args;
                Console.WriteLine("The 7f Xsens DOT has been found");

                if (!_deviceList.ContainsKey(args.Id))
                {
                    _deviceList.Add(args.Id, args);
                }
            }
        }
    }
    
}
