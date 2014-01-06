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
using Heatpump_Control.Resources;


namespace Heatpump_Control
{
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
        private int _pumpTypeIndex = 0;

        // The LoopingSelector data sources
        private ILoopingSelectorDataSource _operatingModes;
        private ILoopingSelectorDataSource _temperatures;
        private ILoopingSelectorDataSource _fanSpeeds;

        // Saved fan speed and temperature for maintenance and fan modes
        private int _fanSpeedSaved = -1;
        private int _temperatureSaved = -1;

        // Public properties 
        [DataMember]
        public string controllerIdentity { get; set; }

        [DataMember]
        private ObservableCollection<HeatpumpModel> heatpumpTypes = new ObservableCollection<HeatpumpModel>();

        [DataMember]
        public NumbersDataSource operatingModes
        {
            get { return (NumbersDataSource)_operatingModes; }
            set { _operatingModes = value; }
        }
        [DataMember]
        public ListLoopingDataSource<int> temperatures
        {
            get { return (ListLoopingDataSource<int>)_temperatures; }
            set { _temperatures = value; }
        }
        [DataMember]
        public ListLoopingDataSource<int> fanSpeeds
        {
            get { return (ListLoopingDataSource<int>)_fanSpeeds; }
            set { _fanSpeeds = value; }
        }

        // Empty constructor for XAML
        public Heatpump()
        {
        }

        // When a new heatpump is created, only its controller identity and supported types are known
        public Heatpump(HeatPumpIdentifyResponse identifyResponse)
        {
            this.controllerIdentity = identifyResponse.identity;
            this.heatpumpTypes = identifyResponse.heatpumpmodels;

            this._powerState = true;
            this._expanded = true;
            this._titleText = "";
            this._pumpTypeIndex = -1;

            // Dummy data for the LoopingSelectors
            this.operatingModes = new NumbersDataSource() { Minimum = HeatpumpModel.MODE_AUTO, Maximum = HeatpumpModel.MODE_AUTO, Default = HeatpumpModel.MODE_AUTO };
            this.temperatures = new ListLoopingDataSource<int>() { Items = Enumerable.Range(0, 1), SelectedItem = 0 };
            this.fanSpeeds = new ListLoopingDataSource<int>() { Items = Enumerable.Range(HeatpumpModel.FAN_AUTO, 1), SelectedItem = HeatpumpModel.FAN_AUTO };
        }

        public void SetNotifications()
        {
            this._operatingModes.SelectionChanged += operatingModes_SelectionChanged;
        }

        // Change the allowed temperature and fan speed values when the mode goes to 'maintenance' or 'fan' mode
        public void operatingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var operatingMode = sender as NumbersDataSource;

            // Maintenance or Fan removed -> restore 'normal' values
            if ((int)(e.RemovedItems[0]) == HeatpumpModel.MODE_MAINT || (int)(e.RemovedItems[0]) == HeatpumpModel.MODE_FAN)
            {
                // Restore the temperatures list in both cases
                this.temperatures.Items = Enumerable.Range(this.heatpumpTypes[_pumpTypeIndex].minTemperature,
                                            (this.heatpumpTypes[_pumpTypeIndex].maxTemperature - this.heatpumpTypes[_pumpTypeIndex].minTemperature) + 1).ToList();
                if (_temperatureSaved >= this.heatpumpTypes[_pumpTypeIndex].minTemperature)
                {
                    this.temperatures.SelectedItem = _temperatureSaved;
                }
                else
                {
                    this.temperatures.SelectedItem = 23;
                }

                // Restore the fan speeds only if going away from maintenance mode
                if ((int)(e.RemovedItems[0]) == HeatpumpModel.MODE_MAINT)
                {
                    // Restore the fan speeds list
                    this.fanSpeeds.Items = Enumerable.Range(HeatpumpModel.FAN_AUTO, this.heatpumpTypes[_pumpTypeIndex].numberOfFanSpeeds).ToList();
                    if (_fanSpeedSaved != -1)
                    {
                        this.fanSpeeds.SelectedItem = _fanSpeedSaved;
                    }
                    else
                    {
                        this.fanSpeeds.SelectedItem = HeatpumpModel.FAN_AUTO;
                    }
                }
            }

            // Maintenance mode was added -> set 'maintenance' values
            if ((int)(e.AddedItems[0]) == HeatpumpModel.MODE_MAINT)
            {
                _fanSpeedSaved = (int)this.fanSpeeds.SelectedItem;
                if ((int)this.temperatures.SelectedItem >= this.heatpumpTypes[_pumpTypeIndex].minTemperature)
                {
                    _temperatureSaved = (int)this.temperatures.SelectedItem;
                }

                this.temperatures.Items = this.heatpumpTypes[this.selectedTypeIndex].maintenance;
                this.temperatures.SelectedItem = this.heatpumpTypes[this.selectedTypeIndex].maintenance[0];

                this.fanSpeeds.Items = Enumerable.Range(this.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds - 1, 1).ToList();
                this.fanSpeeds.SelectedItem = this.heatpumpTypes[this.selectedTypeIndex].numberOfFanSpeeds - 1;
            }

            // Fan mode was added -> set 'fan' values
            if ((int)(e.AddedItems[0]) == HeatpumpModel.MODE_FAN)
            {
                if ((int)this.temperatures.SelectedItem >= this.heatpumpTypes[_pumpTypeIndex].minTemperature)
                {
                    _temperatureSaved = (int)this.temperatures.SelectedItem;
                }

                // Set the temperatures list to a list of a single zero -> nothing is shown by the temperature converter
                this.temperatures.Items = Enumerable.Range(0, 1).ToList();
                this.temperatures.SelectedItem = 0;
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

        // The name of the heatpump type, like 'panasonic_ckp'
        public string heatpumpTypeName
        {
            get
            {
                if (selectedTypeIndex != -1)
                {
                    return heatpumpTypes[selectedTypeIndex].model;
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
                    return heatpumpTypes[selectedTypeIndex].displayName;
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
        // The first entry is 'Select...', which forces the user to actually select something
        // -> causes the selectedTypeIndex to fire up
        public List<string> heatpumpTypeNames
        {
            get
            {
                List<String> heatpumpTypeNames = new List<String>();

                foreach (var heatpumpType in heatpumpTypes)
                {
                    heatpumpTypeNames.Add(heatpumpType.displayName);
                }

                return heatpumpTypeNames;
            }
            set
            { }
        }

        // A list of all known heatpump types, in the user-friendly format
        // The first entry is 'Select...', which forces the user to actually select something
        // -> causes the selectedTypeIndex to fire up
        public List<string> heatpumpTypeNamesGUI
        {
            get
            {
                List<String> heatpumpTypeNames = this.heatpumpTypeNames;
                heatpumpTypeNames.Insert(0, AppResources.Select);

                return heatpumpTypeNames;
            }
            set
            { }
        }

        // Set or get the selected type of this heatpump instance
        public int selectedTypeIndexGUI
        {
            get
            {
                if (_pumpTypeIndex == -1)
                {
                    return 0;
                }
                else
                {
                    return _pumpTypeIndex + 1;
                }
            }
            set
            {
                selectedTypeIndex = value - 1;
            }
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

                    // Set the operating modes list
                    this.operatingModes.Minimum = HeatpumpModel.MODE_AUTO;


                    if (this.heatpumpTypes[_pumpTypeIndex].maintenance != null)
                    {
                        this.operatingModes.Maximum = HeatpumpModel.MODE_MAINT;
                    }
                    else
                    {
                        this.operatingModes.Maximum = HeatpumpModel.MODE_FAN;
                    }

                    this.operatingModes.Default = HeatpumpModel.MODE_HEAT;

                    // Set the temperatures list
                    this.temperatures.Items = Enumerable.Range(this.heatpumpTypes[_pumpTypeIndex].minTemperature,
                                                (this.heatpumpTypes[_pumpTypeIndex].maxTemperature - this.heatpumpTypes[_pumpTypeIndex].minTemperature) + 1).ToList();
                    this.temperatures.SelectedItem = 23;

                    // Set the fan speeds list
                    this.fanSpeeds.Items = Enumerable.Range(HeatpumpModel.FAN_AUTO, this.heatpumpTypes[_pumpTypeIndex].numberOfFanSpeeds).ToList();
                    this.fanSpeeds.SelectedItem = HeatpumpModel.FAN_AUTO;

                    // Send the notification about changed data
                    NotifyPropertyChanged("operatingModes");
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
