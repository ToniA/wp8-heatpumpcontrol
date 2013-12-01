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

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Specialized;


namespace Heatpump_Control
{

    [DataContract]
    public class HeatpumpType
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string displayName { get; set; }

        public HeatpumpType()
        {
        }

        public HeatpumpType(string name, string displayName)
        {
            this.name = name;
            this.displayName = displayName;
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

        public void Add(HeatpumpType heatpumpType)
        {
            this.heatpumpTypes.Add(heatpumpType);
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

        // Public properties 
        [DataMember]
        public string controllerIdentity { get; set; }
        [DataMember]
        public NumbersDataSource operatingModes { get; set; }
        [DataMember]
        public NumbersDataSource temperatures { get; set; }
        [DataMember]
        public NumbersDataSource fanSpeeds { get; set; }


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
            this.temperatures = new NumbersDataSource() { Minimum = 16, Maximum = 30, Default = 23 };
            this.fanSpeeds = new NumbersDataSource() { Minimum = 1, Maximum = 6, Default = 1 };

            // In a later phase the idea is that the controller tells which types it supports
            // For now, let's just hardwire them here

            heatpumpTypes.heatpumpTypes.Add(new HeatpumpType("panasonic_ckp", "Panasonic CKP"));
            heatpumpTypes.heatpumpTypes.Add(new HeatpumpType("panasonic_dke", "Panasonic DKE"));
            heatpumpTypes.heatpumpTypes.Add(new HeatpumpType("midea", "Ultimate Pro Plus 13FP"));
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
                    NotifyPropertyChanged("heatpumpType");
                    NotifyPropertyChanged("heatpumpTypeName");
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
