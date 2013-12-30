using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace Heatpump_Control
{
    public delegate void receiveCommandHandler(string message);

    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Heatpump> _Heatpumps;

        public MainViewModel()
        {
            this.heatpumps = new ObservableCollection<Heatpump>();
            this.settings = new Settings();
            this.notificationHandlers = new Dictionary<string, receiveCommandHandler>();
        }

        public Settings settings { get; set; }
        public Dictionary<string, receiveCommandHandler> notificationHandlers { get; set; }
        public ObservableCollection<Heatpump> heatpumps
        {
            get { return _Heatpumps; }
            set
            {
                _Heatpumps = value;
                NotifyPropertyChanged("Heatpumps");
                _Heatpumps.CollectionChanged += ContentCollectionChanged;
            }
        }


        public void ContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("Heatpumps"));
        }

        // get the given heatpump
        public Heatpump getHeatPumpListItem(string TitleText)
        {
            return heatpumps.SingleOrDefault(item => item.titleText == TitleText);
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
