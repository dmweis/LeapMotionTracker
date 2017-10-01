using System.Numerics;
using System.Text;
using Leap;
using Newtonsoft.Json;

namespace LeapTest
{
    class DeviceTrackingData
    {
        public int DeviceIndex { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public DeviceTrackingData(Hand hand)
        {
            DeviceIndex = hand.Id;
            // leap reports in mm but we want cm so divide by 10
            Position = hand.PalmPosition.ToVector3() / 10;
            Rotation = Quaternion.Identity;
        }

        public DeviceTrackingData(int deviceIndex, Vector3 position, Quaternion rotation)
        {
            DeviceIndex = deviceIndex;
            Position = position;
            Rotation = rotation;
        }

        public DeviceTrackingData(int deviceIndex, Vector3 position)
        {
            DeviceIndex = deviceIndex;
            Position = position;
            Rotation = Quaternion.Identity;
        }

        public byte[] Serealize()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }
    }
}
