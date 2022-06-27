using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

/*
 NOTE :
 Make XSENS into object
 Refer to the name rather than the ID
*/

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

            // Query for extra properties you want returned
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };



            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // GAP DISCOVERY :
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            Printer.Cyan(">> Discovering all BLE devices nearby");

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




            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // GATT CONNECTION :
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
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



            // Listing all the services
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



            // TEST: Write to identify XSENS
            //await writeXSENS_TEST(_connectedBLEDevices);


            // TEST: Listing device info
            //await readAllXSENS_DeviceInfo(_connectedBLEDevices);

            // TEST: Listing battery info
            //await readAllXSENS_BatteryData(_connectedBLEDevices);

            // TEST: Listing XSENS control info
            //await readAllXSENS_Control(_connectedBLEDevices);



            // TEST: Stream Data
            try
            {
                await stream_XSENS_Data_Test(_connectedBLEDevices);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            





            // Endline
            Console.WriteLine("Press any to exit");
            Console.ReadKey();
        }



        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Stream Data (TEST)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task stream_XSENS_Data_Test(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            int count = 1;
            BluetoothLEDevice XSENS_Device = null;
            bool receive = false;

            // --------------------------------------------------------------------------------------------------------------------------------------------
            // Grab one XSENS :
            // --------------------------------------------------------------------------------------------------------------------------------------------
            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                if (count == 1)
                {
                    XSENS_Device = entry.Value;
                }
                count++;
            }


            await XSENS_Blink(XSENS_Device);


            // --------------------------------------------------------------------------------------------------------------------------------------------
            // STEP 0 - Get the "Measurement" Service :
            // --------------------------------------------------------------------------------------------------------------------------------------------

            // Get list of services :
            GattDeviceServicesResult XSENS_ServicesList = await XSENS_Device.GetGattServicesAsync();

            // Get the "Measurement Service" (0x2000) :
            GattDeviceService XSENS_MeasurementService = getXSENS_Service(XSENS_ServicesList, "15172000-4947-11e9-8646-d663bd873d93");

            // Get list of characteristics :
            GattCharacteristicsResult XSENS_characteristicsList = await XSENS_MeasurementService.GetCharacteristicsAsync();




            // --------------------------------------------------------------------------------------------------------------------------------------------
            // STEP 1 - Subscribe to notification :
            // --------------------------------------------------------------------------------------------------------------------------------------------

            // Get the "Medium payload length" characteristic :
            GattCharacteristic XSENS_MediumPayloadLength = getXSENS_Characteristic(XSENS_characteristicsList, "15172003-4947-11e9-8646-d663bd873d93");


            if (XSENS_MediumPayloadLength.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                Printer.Blue($"\t INFO: This characteristic supports subscribing to notifications");


                GattCommunicationStatus status = await XSENS_MediumPayloadLength.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (status == GattCommunicationStatus.Success)
                {
                    Printer.Blue($"\t INFO: Receiving notifications from device!");
                    receive = true;
                }
                else
                {
                    Printer.Red($"\t INFO: status");
                }
            }



            // --------------------------------------------------------------------------------------------------------------------------------------------
            // STEP 2 - Set payload mode + start streaming :
            // --------------------------------------------------------------------------------------------------------------------------------------------

            // Get the "Control" characteristic :
            GattCharacteristic XSENS_ControlCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15172001-4947-11e9-8646-d663bd873d93");


            // Read the existing data byte Array :
            GattReadResult XSENS_DataStruct = await XSENS_ControlCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);

            Printer.White($"\n");
            Printer.White($"\t Received Byte Array            : {dataStruct.Length}-bytes -> " + String.Join(" ", dataStruct) + "\n");
            Printer.White($"\t Type                           : {dataStruct[0]}");
            Printer.White($"\t Action                         : {dataStruct[1]}");
            Printer.White($"\t Payload                        : {dataStruct[2]}");
            Printer.White($"\n");




            // Edit and Write data struct
            dataStruct[1] = 1;
            dataStruct[2] = 16;
            await XSENS_ControlCharacteristic.WriteValueAsync(dataStruct.AsBuffer());




            // Read the edited data byte Array :
            XSENS_DataStruct = await XSENS_ControlCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            dataStruct = getXSENS_DataArray(XSENS_DataStruct);

            Printer.White($"\t Edited Byte Array              : {dataStruct.Length}-bytes -> " + String.Join(" ", dataStruct) + "\n");
            Printer.White($"\t Type                           : {dataStruct[0]}");
            Printer.White($"\t Action                         : {dataStruct[1]}");
            Printer.White($"\t Payload                        : {dataStruct[2]}");
            Printer.White($"\n");



            XSENS_MediumPayloadLength.ValueChanged += receiveDataAsync;




            //while (receive)
            //{
            //XSENS_MediumPayloadLength.ValueChanged += receiveDataAsync;
            //Block code
            //XSENS_MediumPayloadLength.ValueChanged += receiveDataAsync;
            //}





            // --------------------------------------------------------------------------------------------------------------------------------------------
            // STEP 3 - BLE notification :
            // --------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------------------------------------------------------------------------------------------------------
            // STEP 4 - Stop streaming :
            // --------------------------------------------------------------------------------------------------------------------------------------------

        }




        private static void receiveDataAsync(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] dataStruct = args.CharacteristicValue.ToArray();
            //Printer.White($"\t >> : {dataStruct.Length}-bytes -> " + String.Join(" ", dataStruct) + "\n");


            byte[] timeStampSubArr = getSubArray(dataStruct, 0, 4);

            byte[] eulerSubArrX = getSubArray(dataStruct, 4, 4);
            byte[] eulerSubArrY = getSubArray(dataStruct, 8, 4);
            byte[] eulerSubArrZ = getSubArray(dataStruct, 12, 4);

            byte[] accnSubArrX = getSubArray(dataStruct, 16, 4);
            byte[] accnSubArrY = getSubArray(dataStruct, 20, 4);
            byte[] accnSubArrZ = getSubArray(dataStruct, 24, 4);


            //Printer.White($"\t Timestamp    : {(double)BitConverter.ToUInt32(timeStampSubArr, 0)}"  );
            Printer.White($"\t Euler >     X: {BitConverter.ToSingle(eulerSubArrX, 0)}, Y: {BitConverter.ToSingle(eulerSubArrY, 0)}, Z: {BitConverter.ToSingle(eulerSubArrZ, 0)}");
            //Printer.White($"\t Accn  >     X: {BitConverter.ToSingle(accnSubArrX, 0)}, Y: {BitConverter.ToSingle(accnSubArrY, 0)}, Z: {BitConverter.ToSingle(accnSubArrZ, 0)}");

        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Read "Measurement Service > Control Characteristics" (TEST)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task readAllXSENS_Control(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            // Listing the battery details
            Printer.Cyan(">> Listing the Sensor basic information");

            int count = 1;

            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");


                // Get list of services :
                GattDeviceServicesResult XSENS_ServicesList = await entry.Value.GetGattServicesAsync();

                // Get the "Measurement Service" (0x2000) :
                GattDeviceService XSENS_MeasurementService = getXSENS_Service(XSENS_ServicesList, "15172000-4947-11e9-8646-d663bd873d93");

                // Get list of characteristics :
                GattCharacteristicsResult XSENS_characteristicsList = await XSENS_MeasurementService.GetCharacteristicsAsync();

                // Get the "Control" characteristic :
                GattCharacteristic XSENS_ControlCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15172001-4947-11e9-8646-d663bd873d93");

                // Read the data byte Array :
                GattReadResult XSENS_DataStruct = await XSENS_ControlCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);

                Printer.White($"  Received Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");

                Printer.White($"\t Type                           : {dataStruct[0]}");
                Printer.White($"\t Action                         : {dataStruct[1]}");
                Printer.White($"\t Payload                        : {dataStruct[2]}");
                Printer.White($"\n");

                count++;
            }
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Read "Device control " Characteristics (TEST)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task readAllXSENS_DeviceControl(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            // Listing the battery details
            Printer.Cyan(">> Listing the Sensor basic information");

            int count = 1;

            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");


                // Get list of services :
                GattDeviceServicesResult XSENS_ServicesList = await entry.Value.GetGattServicesAsync();

                // Get the "Configuration Service" (0x1000) :
                GattDeviceService XSENS_BatteryService = getXSENS_Service(XSENS_ServicesList, "15171000-4947-11e9-8646-d663bd873d93");


                // Get list of characteristics :
                GattCharacteristicsResult XSENS_characteristicsList = await XSENS_BatteryService.GetCharacteristicsAsync();

                // Get the "Device info" characteristic :
                GattCharacteristic XSENS_BatteryCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15171002-4947-11e9-8646-d663bd873d93");


                // Get list of results :
                GattReadResult XSENS_DataStruct = await XSENS_BatteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);



                // Get the data structure :
                byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);
                Printer.White($"  Received Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");


                // Get sub array :
                byte[] deviceTagSubArr = getSubArray(dataStruct, 8, 16);
                byte[] outputRateSubArr = getSubArray(dataStruct, 24, 2);
                byte[] reservedSubArr = getSubArray(dataStruct, 26, 5);

                Printer.White($"\t Visit Index                  : {dataStruct[0]}");
                Printer.White($"\t Identifying                  : {dataStruct[1]}");
                Printer.White($"\t Power off/on options         : {dataStruct[2]}");
                Printer.White($"\t Power saving timeout X (min) : {dataStruct[3]}");
                Printer.White($"\t Power saving timeout X (sec) : {dataStruct[4]}");
                Printer.White($"\t Power saving timeout Y (min) : {dataStruct[5]}");
                Printer.White($"\t Power saving timeout Y (sec) : {dataStruct[6]}");
                Printer.White($"\t Device Tag length            : {dataStruct[7]}");
                Printer.White($"\t Device Tag                   : {Encoding.Default.GetString(deviceTagSubArr)}");
                Printer.White($"\t Output rate                  : {BitConverter.ToInt16(outputRateSubArr, 0)}");
                Printer.White($"\t Filter profile index         : {dataStruct[26]}");
                Printer.White($"\t Reserved                     : {String.Join(" ", reservedSubArr)}");
                Printer.White($"\n");

                count++;
            }
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Read "Device info" Characteristics (TEST)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task readAllXSENS_DeviceInfo(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            // Listing the battery details
            Printer.Cyan(">> Listing the Sensor basic information");

            int count = 1;

            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");


                // Get list of services :
                GattDeviceServicesResult XSENS_ServicesList = await entry.Value.GetGattServicesAsync();

                // Get the "Configuration Service" (0x1000) :
                GattDeviceService XSENS_BatteryService = getXSENS_Service(XSENS_ServicesList, "15171000-4947-11e9-8646-d663bd873d93");


                // Get list of characteristics :
                GattCharacteristicsResult XSENS_characteristicsList = await XSENS_BatteryService.GetCharacteristicsAsync();

                // Get the "Device info" characteristic :
                GattCharacteristic XSENS_BatteryCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15171001-4947-11e9-8646-d663bd873d93");


                // Get list of results :
                GattReadResult XSENS_DataStruct = await XSENS_BatteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);



                // Get the data structure :
                byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);
                Printer.White($"  Received Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");


                // Get sub array :
                byte[] yearSubArr = getSubArray(dataStruct, 9, 2);
                byte[] SoftDeviceSubArr = getSubArray(dataStruct, 16, 4);
                byte[] SerialNumber = getSubArray(dataStruct, 20, 8);
                byte[] ShortProductCodeSubArr = getSubArray(dataStruct, 28, 6);

                Printer.White($"\t Built Year         : {BitConverter.ToUInt16(yearSubArr, 0)}");
                Printer.White($"\t Build Month        : {dataStruct[11]}");
                Printer.White($"\t Build Date         : {dataStruct[12]}");
                Printer.White($"\t Build Hour         : {dataStruct[13]}");
                Printer.White($"\t Build Minute       : {dataStruct[14]}");
                Printer.White($"\t Build second       : {dataStruct[15]}");
                Printer.White($"\t SoftDevice version : {BitConverter.ToUInt32(SoftDeviceSubArr, 0)}");
                Printer.White($"\t Serial Number      : {BitConverter.ToUInt64(SerialNumber, 0)}");
                Printer.White($"\t Short Product Code : {Encoding.Default.GetString(ShortProductCodeSubArr)}");
                Printer.White($"\n");

                count++;
            }
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Read "battery" Characteristics (TEST)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task readAllXSENS_BatteryData(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            // Listing the battery details
            Printer.Cyan(">> Listing the battery status of the IMUs");

            int count = 1;

            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");


                // Get list of services :
                GattDeviceServicesResult XSENS_ServicesList = await entry.Value.GetGattServicesAsync();

                // Get the battery service :
                GattDeviceService XSENS_BatteryService = getXSENS_Service(XSENS_ServicesList, "15173000-4947-11e9-8646-d663bd873d93");


                // Get list of characteristics :
                GattCharacteristicsResult XSENS_characteristicsList = await XSENS_BatteryService.GetCharacteristicsAsync();

                // Get the battery service characteristic :
                GattCharacteristic XSENS_BatteryCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15173001-4947-11e9-8646-d663bd873d93");


                // Get list of results :
                GattReadResult XSENS_DataStruct = await XSENS_BatteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

                // Get the data structure :
                byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);
                Printer.White($"  Received Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");



                // Extract fields :
                Printer.White($"\t Battery Lv       : {dataStruct[0].ToString()}%");
                Printer.White($"\t ChargingStatus   : {dataStruct[1].ToString()}");
                Printer.White($"\n");

                count++;
            }
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Write Test (Identifying XSENS)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task writeXSENS_TEST(ConcurrentDictionary<String, BluetoothLEDevice> BLEDevicesList)
        {
            int count = 1;

            foreach (KeyValuePair<string, BluetoothLEDevice> entry in BLEDevicesList)
            {
                Printer.Magenta($">>> IMU {count} | {_uncoveredXsensDot[entry.Key].Id}");


                // Get list of services :
                GattDeviceServicesResult XSENS_ServicesList = await entry.Value.GetGattServicesAsync();

                // Get the "Configuration Service" (0x1000) :
                GattDeviceService XSENS_BatteryService = getXSENS_Service(XSENS_ServicesList, "15171000-4947-11e9-8646-d663bd873d93");

                // Get list of characteristics :
                GattCharacteristicsResult XSENS_characteristicsList = await XSENS_BatteryService.GetCharacteristicsAsync();

                // Get the "Device info" characteristic :
                GattCharacteristic XSENS_BatteryCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15171002-4947-11e9-8646-d663bd873d93");

                // Get list of results :
                GattReadResult XSENS_DataStruct = await XSENS_BatteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);



                // Get the data structure :
                byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);
                Printer.White($"  Received Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");


                if (count > 0)
                {
                    // WRITE TEST:

                    //byte[] editedDataStruct = new byte[] { 1, 1, 2, 10, 0, 30, 0, 9, 88, 115, 101, 110, 115, 32, 68, 79, 84, 0, 0, 0, 0, 0, 0, 0, 60, 0, 0, 0, 0, 0, 0, 0 };
                    // Edit and Write data struct
                    dataStruct[1] = 0x01;
                    await XSENS_BatteryCharacteristic.WriteValueAsync(dataStruct.AsBuffer());

                    Printer.White($"\t Identifying                  : {dataStruct[1]}");
                    Printer.White($"\n");
                }

                // Get the data structure :
                Printer.White($"  Edited Byte Array : {dataStruct.Length}-bytes \n  " + String.Join(" ", dataStruct) + "\n");


                count++;
            }
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Identifying XSENS by blinking
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static async Task XSENS_Blink(BluetoothLEDevice XSENS_Device)
        {
            // Get list of services :
            GattDeviceServicesResult XSENS_ServicesList = await XSENS_Device.GetGattServicesAsync();

            // Get the "Configuration Service" (0x1000) :
            GattDeviceService XSENS_BatteryService = getXSENS_Service(XSENS_ServicesList, "15171000-4947-11e9-8646-d663bd873d93");

            // Get list of characteristics :
            GattCharacteristicsResult XSENS_characteristicsList = await XSENS_BatteryService.GetCharacteristicsAsync();

            // Get the "Device info" characteristic :
            GattCharacteristic XSENS_BatteryCharacteristic = getXSENS_Characteristic(XSENS_characteristicsList, "15171002-4947-11e9-8646-d663bd873d93");

            // Get list of results :
            GattReadResult XSENS_DataStruct = await XSENS_BatteryCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);

            // Get the data structure :
            byte[] dataStruct = getXSENS_DataArray(XSENS_DataStruct);

            // Edit and Write data struct
            dataStruct[1] = 0x01;
            await XSENS_BatteryCharacteristic.WriteValueAsync(dataStruct.AsBuffer());
        }








        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Make subarray of array
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public static T[] getSubArray<T>(T[] array, int startIdx, int length)
        {
            T[] subArray = new T[length];

            Array.Copy(array, startIdx, subArray, 0, length);

            return subArray;
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Get a service specified by the UUID provided
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static GattDeviceService getXSENS_Service(GattDeviceServicesResult XSENS_ServicesList, string XSENS_serviceUUID)
        {
            GattDeviceService foundService = null;

            // Check GattDeviceServicesResult status :
            if (XSENS_ServicesList.Status == GattCommunicationStatus.Success)
            {

                // Get XSENS list of services :
                IReadOnlyList<GattDeviceService> services = XSENS_ServicesList.Services;


                // Scan XSENS list of services :
                foreach (var service in services)
                {
                    // If service UUID matches, return the service :
                    if (service.Uuid.ToString().Equals(XSENS_serviceUUID))
                    {
                        foundService = service;
                    }
                }
            }

            else
            {
                Printer.Red("Accessing Service Denied, GattDeviceServicesResult unsuccessful!");
            }

            return foundService;
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Get a characteristic specified by the UUID provided
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static GattCharacteristic getXSENS_Characteristic(GattCharacteristicsResult XSENS_Characteristics, string XSENS_characteristicUUID)
        {
            GattCharacteristic foundCharacteristic = null;

            // Check GattCharacteristicsResult status :
            if (XSENS_Characteristics.Status == GattCommunicationStatus.Success)
            {

                // Get XSENS list of characteristics :
                IReadOnlyList<GattCharacteristic> characteristics = XSENS_Characteristics.Characteristics;


                foreach (GattCharacteristic characteristic in XSENS_Characteristics.Characteristics)
                {

                    // If characteristic UUID matches, read the data :
                    if (characteristic.Uuid.ToString().Equals(XSENS_characteristicUUID))
                    {
                        foundCharacteristic = characteristic;
                    }

                }
            }
            else
            {
                Printer.Red("Accessing Characteristice Denied, GattCharacteristicsResult unsuccessful!");
            }

            return foundCharacteristic;
        }


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : Get a data structure (byte[]) from the characteristic provided
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private static byte[] getXSENS_DataArray(GattReadResult XSENS_DataStruct)
        {
            byte[] foundDataStruct = null;

            // Check GattReadResult status :
            if (XSENS_DataStruct.Status == GattCommunicationStatus.Success)
            {
                foundDataStruct = XSENS_DataStruct.Value.ToArray();
            }
            else
            {
                Printer.Red("Accessing Data Struct Denied, GattReadResult unsuccessful!");
            }

            return foundDataStruct;
        }









        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // DESCRIPTION : DeviceWatcher methods
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
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

                //Console.Write($"-> Name: {args.Name}, ID: {args.Id}, IsEnabled: {args.IsEnabled}");

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
