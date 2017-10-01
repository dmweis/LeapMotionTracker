using System;
using System.Numerics;
using System.Text;
using Leap;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace LeapTest
{

    class Program
    {
        private static Vector3 _anchorPosition;
        private static bool _wasFist;
        private static int _lastNewFrame;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            using (IController controller = new Controller())
            {
                channel.ExchangeDeclare("TrackerService", "fanout");
                controller.SetPolicy(Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);

                controller.FrameReady += (sender, eventArgs) =>
                {
                    if (Environment.TickCount - _lastNewFrame < 20)
                    {
                        return;
                    }
                    _lastNewFrame = Environment.TickCount;

                    Frame frame = eventArgs.frame;
                    if (frame.Hands.Count == 0)
                    {
                        Console.WriteLine("No hands detected!");
                        channel.BasicPublish("TrackerService", string.Empty, null, new DeviceTrackingData(0, new Vector3(0, 0, 0)).Serealize());
                        _wasFist = false;
                    }
                    else if (frame.Hands.Count > 1)
                    {
                        Console.WriteLine("More than 1 hand detected!");
                        channel.BasicPublish("TrackerService", string.Empty, null, new DeviceTrackingData(0, new Vector3(0, 0, 0)).Serealize());
                        _wasFist = false;
                    }
                    else
                    {
                        Hand hand = frame.Hands[0];
                        Vector3 psotion = hand.PalmPosition.ToVector3() / 10;
                        bool isFist = hand.GrabAngle.RadToDeg() > 160;

                        const float offsetLimit = 18;
                        const float offsetHeight = 38;

                        if (Math.Abs(psotion.X) > offsetLimit || Math.Abs(psotion.Z) > offsetLimit || psotion.Y > offsetHeight)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"hadn out of bounds! {psotion}");
                            Console.ResetColor();
                            _wasFist = false;
                            channel.BasicPublish("TrackerService", string.Empty, null, new DeviceTrackingData(hand.Id, new Vector3(0, 0, 0)).Serealize());
                            return;
                        }
                        if (isFist)
                        {
                            if (!_wasFist)
                            {
                                _wasFist = true;
                                _anchorPosition = psotion;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"hand closed! New anchor: {_anchorPosition}");
                                Console.ResetColor();
                            }
                            if (hand.Confidence < 0.9)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Not confident!");
                                Console.ResetColor();
                                channel.BasicPublish("TrackerService", string.Empty, null, new DeviceTrackingData(hand.Id, new Vector3(0, 0, 0)).Serealize());
                                return;
                            }
                            Vector3 correctedPosition = psotion - _anchorPosition;
                            Console.WriteLine($"Fist pos: {correctedPosition}");
                            channel.BasicPublish("TrackerService", string.Empty, null, new DeviceTrackingData(hand.Id, correctedPosition).Serealize());
                        }
                        else
                        {
                            if (_wasFist)
                            {
                                _wasFist = false;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("hand opened!");
                                Console.ResetColor();
                                string json = JsonConvert.SerializeObject(new DeviceTrackingData(hand.Id, new Vector3(0, 0, 0)));
                                channel.BasicPublish("TrackerService", string.Empty, null, Encoding.UTF8.GetBytes(json));
                            }
                            Console.WriteLine($"Hand is open pos: {psotion}");
                        }
                    }
                };
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Closing!");
                Console.ResetColor();
            }
        }
    }

    static class Extensions
    {
        public static Vector3 ToVector3(this Leap.Vector leapVector)
        {
            return new Vector3(leapVector.x, leapVector.y, leapVector.z);
        }

        public static float RadToDeg(this float number)
        {
            return number * 180.0f / (float) Math.PI;
        }
    }
}
