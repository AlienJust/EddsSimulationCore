using Controllers.Gateway.Attached;

namespace Controllers.Lora
{
    class LoraControllerInfoSimple
    {
        public LoraControllerInfoSimple(string name, string deviceId)
        {
            Name = name;
            DeviceId = deviceId;
        }

        public string Name { get; }
        public string DeviceId { get; }
    }
}