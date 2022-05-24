using System;
using Windows.Devices.Enumeration;

namespace Testing
{
    public class SensorModel
    {
        public String Name { get; set; }
        public DeviceInformation DeviceInformation { get; set; }
        public String SignalDetails { get; set; }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public String ToString()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            return $"DeviceName: {DeviceInformation.Name}, DeviceID: {DeviceInformation.Id},";
        }

    }
}
