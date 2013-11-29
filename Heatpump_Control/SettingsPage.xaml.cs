using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace Heatpump_Control
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
        }

        // How to save the settings into Isolated storage
        private void SaveSettings_Click(object sender, EventArgs e)
        {
            App.ViewModel.settings.Save();

            Dispatcher.BeginInvoke(() =>
              MessageBox.Show("Asetukset talletettu")
            );

            NavigationService.GoBack();
        }

        // Wipe clean the Isolated Storage
        private void WipeSettings_Click(object sender, EventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Clear();

            Dispatcher.BeginInvoke(() =>
              MessageBox.Show("Oletusasetukset palautettu")
            );
        }
    }
}