using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Notification;


namespace Heatpump_Control
{
    public class Settings
    {
        public Settings()
        {
            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        // Our isolated storage settings
        public IsolatedStorageSettings settings;

        // The Push URL
        public HttpNotificationChannel pushChannel;

        // The isolated storage key names of our settings
        const string SSIDKeyName = "SSIDSetting";
        const string UDPBroadcastKeyName = "UDPBroadcastSetting";
        const string SSHServerKeyName = "SSHServerSetting";
        const string SSHPortKeyName = "SSHPortSetting";
        const string SSHAccountKeyName = "SSHAccountSetting";
        const string SSHUsePasswordKeyName = "SSHUsePasswordSetting";
        const string SSHPasswordKeyName = "SSHPasswordSetting";
        const string SSHUseKeyKeyName = "SSHUseKeySetting";
        const string SSHKeyKeyName = "SSHKeySetting";
        const string SettingsSavedKeyName = "SettingsSavedSetting";
        const string HeatpumpsSettingsKeyName = "HeatpumpsSettingsSetting";


        // The default values of our settings
        const string SSIDSettingDefault = "Home SSID";
        const string UDPBroadcastSettingDefault = "echo '{0}' | LD_LIBRARY_PATH=\\$LD_LIBRARY_PATH:/jffs/usr/lib socat -v - UDP4:192.168.0.255:49722,broadcast";
        const string SSHServerSettingDefault = "myhome.dy.fi";
        const string SSHPortSettingDefault = "22";
        const string SSHAccountSettingDefault = "root";
        const Boolean SSHUsePasswordSettingDefault = false;
        const Boolean SSHUseKeySettingDefault = true;
        const string SSHPasswordSettingDefault = "password";
        const string SSHKeySettingDefault =
"-----BEGIN RSA PRIVATE KEY-----\n" +
"-----END RSA PRIVATE KEY-----\n";
        const Boolean SettingsSavedSettingDefault = false;
        const string HeatpumpsSettingsSettingDefault = "[]";

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Contains(Key))
            {
                // If the value has changed
                if (settings[Key] != value)
                {
                    // Store the new value
                    settings[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Add(Key, value);
                valueChanged = true;
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValueOrDefault<T>(string Key, T defaultValue)
        {
            T value;

            // If the key exists, retrieve the value.
            if (settings.Contains(Key))
            {
                value = (T)settings[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Save the settings.
        /// </summary>
        public void Save()
        {
            SettingsSavedSetting = true;
            settings.Save();
        }

        public string SSIDSetting
        {
            get
            {
                return GetValueOrDefault<string>(SSIDKeyName, SSIDSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSIDKeyName, value);
            }
        }

        public string UDPBroadcastSetting
        {
            get
            {
                return GetValueOrDefault<string>(UDPBroadcastKeyName, UDPBroadcastSettingDefault);
            }
            set
            {
                AddOrUpdateValue(UDPBroadcastKeyName, value);
            }
        }

        public bool SSHUsePasswordSetting
        {
            get
            {
                return GetValueOrDefault<bool>(SSHUsePasswordKeyName, SSHUsePasswordSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHUsePasswordKeyName, value);
            }
        }

        public bool SSHUseKeySetting
        {
            get
            {
                return GetValueOrDefault<bool>(SSHUseKeyKeyName, SSHUseKeySettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHUseKeyKeyName, value);
            }
        }

        public string SSHServerSetting
        {
            get
            {
                return GetValueOrDefault<string>(SSHServerKeyName, SSHServerSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHServerKeyName, value);
            }
        }

        public string SSHPortSetting
        {
            get
            {
                return GetValueOrDefault<string>(SSHPortKeyName, SSHPortSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHPortKeyName, value);
            }
        }


        public string SSHAccountSetting
        {
            get
            {
                return GetValueOrDefault<string>(SSHAccountKeyName, SSHAccountSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHAccountKeyName, value);
            }
        }


        public string SSHPasswordSetting
        {
            get
            {
                return GetValueOrDefault<string>(SSHPasswordKeyName, SSHPasswordSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SSHPasswordKeyName, value);
            }
        }

        public string SSHKeySetting
        {
            get
            {
                return GetValueOrDefault<string>(SSHKeyKeyName, SSHKeySettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(SSHKeyKeyName, value))
                {
                    Save();
                }
            }
        }

        public string HeatpumpsSettings
        {
            get
            {
                return GetValueOrDefault<string>(HeatpumpsSettingsKeyName, HeatpumpsSettingsSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(HeatpumpsSettingsKeyName, value))
                {
                    Save();
                }
            }
        }


        public bool SettingsSavedSetting
        {
            get
            {
                return GetValueOrDefault<bool>(SettingsSavedKeyName, SettingsSavedSettingDefault);
            }
            set
            {
                AddOrUpdateValue(SettingsSavedKeyName, value);
            }
        }
    }
}
