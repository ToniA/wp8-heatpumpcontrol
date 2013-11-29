using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Heatpump_Control
{
    [DataContract]
    public class HeatPumpIdentifyCommand
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string channel { get; set; }
    }

    [DataContract]
    public class HeatPumpIdentifyResponse
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string identity { get; set; }
    }

    [DataContract]
    public class HeatPumpStateCommand
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string identity { get; set; }

        [DataMember]
        public string channel { get; set; }

        [DataMember]
        public string model { get; set; }

        [DataMember]
        public int power { get; set; }

        [DataMember]
        public int mode { get; set; }

        [DataMember]
        public int fan { get; set; }

        [DataMember]
        public int temperature { get; set; }
    }


    public static class JsonFunctions
    {
        public static string SerializeToJsonString(object objectToSerialize)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(objectToSerialize.GetType());
                serializer.WriteObject(ms, objectToSerialize);
                ms.Position = 0;

                using (StreamReader reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static HeatPumpIdentifyResponse DeserializeIdentifyCommandFromJsonString(string stringToDeserialize)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(stringToDeserialize)))
            {
                DataContractJsonSerializer deserializer =
                        new DataContractJsonSerializer(typeof(HeatPumpIdentifyResponse));

                return (HeatPumpIdentifyResponse)deserializer.ReadObject(ms);
            }
        }

        public static HeatPumpStateCommand DeserializeStateCommandFromJsonString(string stringToDeserialize)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(stringToDeserialize)))
            {
                DataContractJsonSerializer deserializer =
                        new DataContractJsonSerializer(typeof(HeatPumpStateCommand));

                return (HeatPumpStateCommand)deserializer.ReadObject(ms);
            }
        }


        public static string SerializeHeatpumps(ObservableCollection<Heatpump> heatpumps)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(heatpumps.GetType());
                serializer.WriteObject(ms, heatpumps);
                ms.Position = 0;

                using (StreamReader reader = new StreamReader(ms))
                {
                    string jsonData = reader.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine("Serialized to:\n" + jsonData);

                    return jsonData;
                }
            }
        }


        public static ObservableCollection<Heatpump> DeserializeHeatpumps(string stringToDeserialize)
        {
            System.Diagnostics.Debug.WriteLine("Deserializing:\n" + stringToDeserialize);

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(stringToDeserialize)))
            {
                DataContractJsonSerializer deserializer =
                        new DataContractJsonSerializer(typeof(ObservableCollection<Heatpump>));

                return (ObservableCollection<Heatpump>)deserializer.ReadObject(ms);
            }
        }
    }
}
