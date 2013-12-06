using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Phone.Controls;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Specialized;
using Microsoft.Phone.Controls.Primitives;

namespace Heatpump_Control
{

    [DataContract]
    public class HeatpumpType
    {
        // Panasonic CKP constants
        public const int PanasonicCKPModes = 5;
        public const int PanasonicCKPMaxFan = 6; // AUTO + 5 speeds

        // Panasonic DKE constants
        public const int PanasonicDKEModes = 5;
        public const int PanasonicDKEMaxFan = 6; // AUTO + 5 speeds

        public const int PanasonicMinTemperature = 16;
        public const int PanasonicMaxTemperature = 30;

        // Midea (Ultimate Pro Plus 13 FP) constants
        public const int MideaModes = 6;
        public const int MideaMaxFan = 4; // AUTO + 3 speeds

        public const int MideaMinTemperature = 16;
        public const int MideaMaxTemperature = 30;

        // Carrier constants
        public const int CarrierModes = 5;
        public const int CarrierMaxFan = 6; // AUTO + 5 speeds

        public const int CarrierMinTemperature = 17;
        public const int CarrierMaxTemperature = 30;

        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string displayName { get; set; }
        [DataMember]
        public int numberOfModes { get; set; }
        [DataMember]
        public int minTemperature { get; set; }
        [DataMember]
        public int maxTemperature { get; set; }
        [DataMember]
        public int numberOfFanSpeeds { get; set; }

        public HeatpumpType()
        {
        }

        public HeatpumpType(string name, string displayName, int numberOfModes, int minTemperature, int maxTemperature, int numberOfFanSpeeds)
        {
            this.name = name;
            this.displayName = displayName;
            this.numberOfModes = numberOfModes;
            this.minTemperature = minTemperature;
            this.maxTemperature = maxTemperature;
            this.numberOfFanSpeeds = numberOfFanSpeeds;
        }

        // Without this the ListPicker doesn't show up properly in the designer, but throws
        // "SelectedIndex must always be set to a valid value"
        public override bool Equals(object obj)
        {
            var target = obj as HeatpumpType;
            if (target == null)
                return false;

            if (this.name.Equals(target.name))
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }

    }

    [DataContract]
    [ContentProperty("HeatpumpType")]
    public class HeatpumpTypes
    {
        [DataMember]
        public List<HeatpumpType> heatpumpTypes { get; set; }

        public HeatpumpTypes()
        {
            heatpumpTypes = new List<HeatpumpType>();
        }

        public void Add(string heatpumpTypeName)
        {
            if (heatpumpTypeName.Equals("panasonic_ckp"))
            {
                heatpumpTypes.Add(new HeatpumpType("panasonic_ckp", "Panasonic CKP", 
                                  HeatpumpType.PanasonicCKPModes,
                                  HeatpumpType.PanasonicMinTemperature,
                                  HeatpumpType.PanasonicMaxTemperature,
                                  HeatpumpType.PanasonicCKPMaxFan));
            }
            else if (heatpumpTypeName.Equals("panasonic_dke"))
            {
                heatpumpTypes.Add(new HeatpumpType("panasonic_dke", "Panasonic DKE",
                                  HeatpumpType.PanasonicMinTemperature,
                                  HeatpumpType.PanasonicMaxTemperature,
                                  HeatpumpType.PanasonicDKEModes,
                                  HeatpumpType.PanasonicDKEMaxFan));
            }
            else if (heatpumpTypeName.Equals("midea"))
            {
                heatpumpTypes.Add(new HeatpumpType("midea", "Ultimate Pro Plus 13FP", 
                                  HeatpumpType.MideaModes,
                                  HeatpumpType.MideaMinTemperature,
                                  HeatpumpType.MideaMaxTemperature,
                                  HeatpumpType.MideaMaxFan));
            }
            else if (heatpumpTypeName.Equals("carrier"))
            {
                heatpumpTypes.Add(new HeatpumpType("carrier", "Carrier",
                                  HeatpumpType.CarrierModes,
                                  HeatpumpType.CarrierMinTemperature,
                                  HeatpumpType.CarrierMaxTemperature,
                                  HeatpumpType.CarrierMaxFan));
            }
        }


        public List<string> getHeatpumpTypes()
        {
            return (List<string>)heatpumpTypes.Select(p => p.displayName);
        }
    }

    [DataContract]
    public class Heatpump : INotifyPropertyChanged
    {
        // These have getters and setters because of INotify
        [DataMember]
        private Boolean _powerState = false;
        [DataMember]
        private Boolean _expanded = false;
        [DataMember]
        private string _titleText = "";
        [DataMember]
        private HeatpumpTypes _heatpumpTypes = new HeatpumpTypes();
        [DataMember]
        private int _pumpTypeIndex = 0;

        // Saved while in maintenance heat mode
        private int _temperatureSaved;
        private int _fanSpeedSaved;

        private ILoopingSelectorDataSource _operatingModes;
        private ILoopingSelectorDataSource _temperatures;
        private ILoopingSelectorDataSource _fanSpeeds;

        // Public properties 
        [DataMember]
        public string controllerIdentity { get; set; }

        [DataMember]
        public NumbersDataSource operatingModes
        {
            get { return (NumbersDataSource)_operatingModes; }
            set { _operatingModes = value; }
        }
        [DataMember]
        public NumbersDataSource temperatures
        {
            get { return (NumbersDataSource)_temperatures; }
            set { _temperatures = value; }
        }
        [DataMember]
        public NumbersDataSource fanSpeeds
        {
            get { return (NumbersDataSource)_fanSpeeds; }
            set { _fanSpeeds = value; }
        }

        // Empty constructor for XAML
        public Heatpump()
        {
        }

        // When a new heatpump is created, only it's controller identity is known
        public Heatpump(string controllerIdentity)
        {
            this.controllerIdentity = controllerIdentity;

            this._powerState = true;
            this._expanded = true;
            this._titleText = "";
            this._pumpTypeIndex = 0;

            this.operatingModes = new NumbersDataSource() { Minimum = 1, Maximum = 5, Default = 2 };
            this._operatingModes.SelectionChanged += operatingModes_SelectionChanged;

            this.temperatures = new NumbersDataSource() { Minimum = 16, Maximum = 30, Default = 23 };
            this.fanSpeeds = new NumbersDataSource() { Minimum = 1, Maximum = 6, Default = 1 };

            // In a later phase the idea is that the controller tells which types it supports
            // For now, let's just hardwire them here

            heatpumpTypes.Add("panasonic_ckp");
            heatpumpTypes.Add("panasonic_dke");
            heatpumpTypes.Add("midea");
            heatpumpTypes.Add("carrier");
        }

        public void SetNotifications()
        {
            this._operatingModes.SelectionChanged += operatingModes_SelectionChanged;
        }


        // Change the allowed temperature and fan speed values when the mode goes to 'maintenance'
        public void operatingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var operatingMode = sender as NumbersDataSource;

            if ((int)operatingMode.SelectedItem == 6) // Maintenance mode, only possible temperature is +10 and max fan speed
            {
                this._temperatureSaved = (int)this.temperatures.SelectedItem;
                this.temperatures.Minimum = 10;
                this.temperatures.Maximum = 10;
                this.temperatures.SelectedItem = 10;

                this._fanSpeedSaved = (int)this.fanSpeeds.SelectedItem;
                this.fanSpeeds.Minimum = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds;
                this.fanSpeeds.Maximum = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds;
                this.fanSpeeds.SelectedItem = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds;
            }
            else if ((int)operatingMode.SelectedItem == 5) // FAN mode, do not show the temperature setting in FAN mode
            {
                this._temperatureSaved = (int)this.temperatures.SelectedItem;
                this._fanSpeedSaved = (int)this.fanSpeeds.SelectedItem;
                this.temperatures.Minimum = 0;
                this.temperatures.Maximum = 0;
                this.temperatures.SelectedItem = 0;
            }
            else if ((int)(e.RemovedItems[0]) == 5 || (int)(e.RemovedItems[0]) == 6)
            {
                this.temperatures.Minimum = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].minTemperature;
                this.temperatures.Maximum = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].maxTemperature;
                this.temperatures.SelectedItem = (this._temperatureSaved >= this.temperatures.Minimum) ?
                     this._temperatureSaved : this.temperatures.Default;

                this.fanSpeeds.Minimum = 1;
                this.fanSpeeds.Maximum = this.heatpumpTypes.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds;
                this.fanSpeeds.SelectedItem = (this._fanSpeedSaved < this.fanSpeeds.Maximum) ?
                    this._fanSpeedSaved : this.fanSpeeds.Maximum;
            }

            NotifyPropertyChanged("temperatures");
            NotifyPropertyChanged("fanSpeeds");
        }

        // Whether the pump is in expanded state or not on the HeatpumpControllerPage
        public Boolean expanded
        {
            get { return _expanded; }
            set
            {
                if (_expanded != value)
                {
                    _expanded = value;
                    NotifyPropertyChanged("expanded");
                    NotifyPropertyChanged("notExpanded");
                }
            }
        }

        // Visibility of the fake LoopingSelectors
        public Visibility fakeVisibility
        {
            get { return Visibility.Collapsed; }
            set { }
        }

        // Whether the pump is in collapsed state or not on the HeatpumpControllerPage
        public Boolean notExpanded
        {
            get { return !_expanded; }
            set { }
        }

        // The user-friendly name of the heatpump
        public string titleText
        {
            get { return _titleText; }
            set
            {
                if (!_titleText.Equals(value))
                {
                    _titleText = value;
                    NotifyPropertyChanged("titleText");
                }
            }
        }

        // Whether the power is on or off
        public bool powerState
        {
            get { return _powerState; }
            set
            {
                if (_powerState != value)
                {
                    _powerState = value;
                    NotifyPropertyChanged("powerState");
                }
            }
        }

        // List of all supported heatpump types
        public HeatpumpTypes heatpumpTypes
        {
            get
            {
                return _heatpumpTypes;
            }
            set
            {
                _heatpumpTypes = value;
            }
        }

        // The name of the heatpump type, like 'panasonic_ckp'
        public string heatpumpTypeName
        {
            get
            {
                if (selectedTypeIndex != -1)
                {
                    return heatpumpTypes.heatpumpTypes[selectedTypeIndex].name;
                }
                else
                {
                    return "";
                }
            }
            set { }
        }

        // The user-friendly name of the heatpump type, like 'Panasonic CKP'
        public string heatpumpDisplayName
        {
            get
            {
                if (selectedTypeIndex != -1)
                {
                    return heatpumpTypes.heatpumpTypes[selectedTypeIndex].displayName;
                }
                else
                {
                    return "";
                }
            }
            set
            { }
        }

        // A list of all known heatpump types, in the user-friendly format
        public List<string> heatpumpTypeNames
        {
            get
            {
                List<String> heatpumpTypeNames = new List<String>();

                foreach (HeatpumpType heatpumpType in heatpumpTypes.heatpumpTypes)
                {
                    heatpumpTypeNames.Add(heatpumpType.displayName);
                }

                return heatpumpTypeNames;
            }
            set
            { }
        }

        // Set or get the selected type of this heatpump instance
        public int selectedTypeIndex
        {
            get
            {
                return _pumpTypeIndex;
            }
            set
            {
                if (_pumpTypeIndex != value)
                {
                    _pumpTypeIndex = value;

                    // Only offer maintenance mode on models which support it
                    ((NumbersDataSource)operatingModes).Maximum = heatpumpTypes.heatpumpTypes[value].numberOfModes;

                    if ((int)operatingModes.SelectedItem > ((NumbersDataSource)operatingModes).Maximum)
                    {
                        operatingModes.SelectedItem = ((NumbersDataSource)operatingModes).Maximum;
                    }

                    // Only offer the number of temperatures the model supports
                    ((NumbersDataSource)temperatures).Maximum = heatpumpTypes.heatpumpTypes[value].maxTemperature;
                    ((NumbersDataSource)temperatures).Minimum = heatpumpTypes.heatpumpTypes[value].minTemperature;
                    if ((int)temperatures.SelectedItem > ((NumbersDataSource)temperatures).Maximum)
                    {
                        temperatures.SelectedItem = ((NumbersDataSource)temperatures).Maximum;
                    }
                    if ((int)temperatures.SelectedItem < ((NumbersDataSource)temperatures).Minimum)
                    {
                        temperatures.SelectedItem = ((NumbersDataSource)temperatures).Maximum;
                    }

                    // Only offer the number of fanspeeds the model supports
                    ((NumbersDataSource)fanSpeeds).Maximum = heatpumpTypes.heatpumpTypes[value].numberOfFanSpeeds;
                    if ((int)fanSpeeds.SelectedItem > ((NumbersDataSource)fanSpeeds).Maximum)
                    {
                        fanSpeeds.SelectedItem = ((NumbersDataSource)fanSpeeds).Maximum;
                    }

                    NotifyPropertyChanged("heatpumpType");
                    NotifyPropertyChanged("heatpumpTypeName");
                    NotifyPropertyChanged("heatpumpDisplayName");
                    NotifyPropertyChanged("temperatures");
                    NotifyPropertyChanged("fanSpeeds");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
