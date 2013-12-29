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

    // Represents the 'identify' command from the app to the server
    [DataContract]
    public class HeatPumpIdentifyCommand
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string channel { get; set; }
    }

    // Represents the 'identify' response from the server to the app
    [DataContract]
    public class HeatPumpIdentifyResponse
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string identity { get; set; }

        [DataMember]
        public ObservableCollection<HeatpumpModel> heatpumpmodels { get; set; }
    }

    // Represents a heatpump model in the collection of heatpumps in the 'identify' response
    [DataContract]
    public class HeatpumpModel
    {
        // Power state
        public const int POWER_OFF   = 0;
        public const int POWER_ON    = 1;

        // Operating modes
        public const int MODE_AUTO   = 1;
        public const int MODE_HEAT   = 2;
        public const int MODE_COOL   = 3;
        public const int MODE_DRY    = 4;
        public const int MODE_FAN    = 5;
        public const int MODE_MAINT  = 6;

        // Fan speeds. Note that some heatpumps have less than 5 fan speeds
        public const int FAN_AUTO    = 0;
        public const int FAN_1       = 1;
        public const int FAN_2       = 2;
        public const int FAN_3       = 3;
        public const int FAN_4       = 4;
        public const int FAN_5       = 5;

        // Vertical air directions. Note that these cannot be set on all heat pumps
        public const int VDIR_AUTO   = 0;
        public const int VDIR_MANUAL = 0;
        public const int VDIR_SWING  = 1;
        public const int VDIR_UP     = 2;
        public const int VDIR_MUP    = 3;
        public const int VDIR_MIDDLE = 4;
        public const int VDIR_MDOWN  = 5;
        public const int VDIR_DOWN   = 6;

        // Horizontal air directions. Note that these cannot be set on all heat pumps
        public const int HDIR_AUTO   = 0;
        public const int HDIR_MANUAL = 0;
        public const int HDIR_SWING  = 1;
        public const int HDIR_MIDDLE = 2;
        public const int HDIR_LEFT   = 3;
        public const int HDIR_MLEFT  = 4;
        public const int HDIR_MRIGHT = 5;
        public const int HDIR_RIGHT  = 6;

        [DataMember(Name = "mdl", IsRequired = true)]
        public string model { get; set; }

        [DataMember(Name = "dn", IsRequired = true)]
        public string displayName { get; set; }

        [DataMember(Name = "mds", IsRequired = true)]
        public int numberOfModes { get; set; }

        [DataMember(Name = "mT", IsRequired = true)]
        public int minTemperature { get; set; }

        [DataMember(Name = "xT", IsRequired = true)]
        public int maxTemperature { get; set; }

        [DataMember(Name = "fs", IsRequired = true)]
        public int numberOfFanSpeeds { get; set; }

        [DataMember(Name = "maint", IsRequired = false)]
        public ObservableCollection<int> maintenance { get; set; }
    }

    // Represents the 'command' command from the app to the server, and its response from the server to the app
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


    // JSON serialization / deserialization functions

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

        public static Object DeserializeFromStringToJson(string stringToDeserialize, Type type)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(stringToDeserialize)))
            {
                DataContractJsonSerializer deserializer =
                        new DataContractJsonSerializer(type);

                return deserializer.ReadObject(ms);
            }
        }
    }
}
