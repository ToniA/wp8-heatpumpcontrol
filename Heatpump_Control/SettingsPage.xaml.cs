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
using Heatpump_Control.Resources;

namespace Heatpump_Control
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Localize the appbar, this cannot be done with XAML bindings
            var appBarButtons = this.ApplicationBar.Buttons as IList<IApplicationBarIconButton>;
            appBarButtons[0].Text = AppResources.Save;

            var appBarMenuItems = this.ApplicationBar.MenuItems as IList<IApplicationBarMenuItem>;
            appBarMenuItems[0].Text = AppResources.WipeSettings;
        }

        // How to save the settings into Isolated storage
        private void SaveSettings_Click(object sender, EventArgs e)
        {
            App.ViewModel.settings.Save();

            Dispatcher.BeginInvoke(() =>
              MessageBox.Show(AppResources.SettingsSaved)
            );

            NavigationService.GoBack();
        }

        // Wipe clean the Isolated Storage
        private void WipeSettings_Click(object sender, EventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Clear();

            Dispatcher.BeginInvoke(() =>
              MessageBox.Show(AppResources.SettingsWiped)
            );
        }
    }
}