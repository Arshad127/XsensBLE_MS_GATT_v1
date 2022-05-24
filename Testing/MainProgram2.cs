namespace Testing
{
    /*
    class MainProgram2
    {
        
        // CLASS FIELDS
        private static DeviceInformation _device = null;
        private static Dictionary<String, DeviceInformation> _deviceList = new Dictionary<String, DeviceInformation>();
        private static Dictionary<String, SensorModel> _deviceListDetailed = new Dictionary<String, SensorModel>();


        private static GattDeviceService _batteryService;
        private static GattCharacteristic _batteryCharacteristic;

        // CLASS CONSTANTS
        private const string SearchStr = "Xsens DOT";

        public static void Main(string[] args)
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
            Console.WriteLine("Scanning started... ");
            deviceWatcher.Start();

            
            //while (_device == null)
            while(_deviceList.Count <= 2)
            {
                //Thread.Sleep(200);
                //Console.WriteLine($"Searching for {SearchStr} products in the vicinity....");
            }
            

            Thread.Sleep(8000);
            deviceWatcher.Stop();
            Console.WriteLine("Scanning Complete... ");

            Console.ReadKey();
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
            //Console.WriteLine($"Name: {args.Name}, ID: {args.Id}");


            if (args.Name.Equals(SearchStr))
            {
                Console.WriteLine($"Name: {args.Name}, ID: {args.Id}");

                _device = args;

                if (!_deviceListDetailed.ContainsKey(args.Id))
                {
                    
                    SensorModel tempSensor = new SensorModel(
                    {
                        Name = null,
                        DeviceInformation = null,
                        SignalDetails = null
                    });
                    

                    
                    _deviceListDetailed.Add(args.Id, new SensorModel(
                        {
                            Name = args.Name,
                            DeviceInformation = args
                        }
                    ));
                    
                }
            }
        }

    
    }
    */


}
