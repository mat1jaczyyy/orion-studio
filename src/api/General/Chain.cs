using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api.Devices;

namespace api {
    public class Chain {
        private List<Device> _devices = new List<Device>();
        private Action<Signal> _chainenter = null;
        private Action<Signal> _midiexit = null;

        private void Reroute() {
            if (_devices.Count == 0)
                _chainenter = _midiexit;

            else {
                _chainenter = _devices[0].MIDIEnter;
                
                for (int i = 1; i < _devices.Count; i++) {
                    _devices[i - 1].MIDIExit = _devices[i].MIDIEnter;
                }
                
                _devices[_devices.Count - 1].MIDIExit = _midiexit;
            }

            for (int i = 0; i < _devices.Count; i++)
                _devices[i].Parent = this;
        }

        public Device this[int index] {
            get {
                return _devices[index];
            }
            set {
                _devices[index] = value;
                Reroute();
            }
        }

        public int Count {
            get {
                return _devices.Count;
            }
        }

        public Action<Signal> MIDIExit {
            get {
                return _midiexit;
            }
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public Chain Clone() {
            return new Chain((from i in _devices select i.Clone()).ToList());
        }

        public void Insert(int index, Device device) {
            _devices.Insert(index, device);
            Reroute();
        }

        public void Add(Device device) {
            _devices.Add(device);
            Reroute();
        }

        public void Add(Device[] devices) {
            foreach (Device device in devices)
                _devices.Add(device);
            Reroute();
        }

        public void Remove(int index) {
            _devices.RemoveAt(index);
            Reroute();
        }

        public Chain() {}

        public Chain(Device[] init) {
            _devices = init.ToList();
            Reroute();
        }

        public Chain(List<Device> init) {
            _devices = init;
            Reroute();
        }

        public Chain(Action<Signal> exit) {
            _midiexit = exit;
            Reroute();
        }

        public Chain(Device[] init, Action<Signal> exit) {
            _devices = init.ToList();
            _midiexit = exit;
            Reroute();
        }

        public Chain(List<Device> init, Action<Signal> exit) {
            _devices = init;
            _midiexit = exit;
            Reroute();
        }

        public void MIDIEnter(Signal n) {
            if (_chainenter != null)
                _chainenter(n);
        }

        public static Chain Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "chain") return null;

            List<object> data = JsonConvert.DeserializeObject<List<object>>(json["data"].ToString());
            
            List<Device> init = new List<Device>();
            foreach (object device in data) {
                init.Add(Device.Decode(device.ToString()));
            }
            return new Chain(init);
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("chain");

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < _devices.Count; i++) {
                            writer.WriteRawValue(_devices[i].Encode());
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public ObjectResult Request(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != "chain") return new BadRequestObjectResult("Incorrect recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    switch (data["forward"].ToString()) {
                        case "device":
                            return _devices[Convert.ToInt32(data["index"])].RequestSpecific(data["message"].ToString());
                        
                        default:
                            return new BadRequestObjectResult("Incorrectly formatted message.");
                    }
                
                case "add":
                    foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where (type.Namespace.StartsWith("api.Devices") && !type.Namespace.StartsWith("api.Devices.Device")) select type)) {
                        if (device.Name.ToLower().Equals(data["device"])) {
                            Insert(Convert.ToInt32(data["index"]), (Devices.Device)Activator.CreateInstance(device));
                            return new OkObjectResult(_devices[Convert.ToInt32(data["index"])].Encode());
                        }
                    }
                    return new BadRequestObjectResult("Incorrectly formatted message.");

                case "remove":
                    Remove(Convert.ToInt32(data["index"]));
                    return new OkObjectResult(null);
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}