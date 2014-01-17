using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Notification;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ReorderListBox;
using Heatpump_Control.Resources;

namespace Heatpump_Control
{
    public partial class MainPage : PhoneApplicationPage
    {
        public string SSID;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Load heatpumps from the settings data
            App.ViewModel.heatpumps = (ObservableCollection<Heatpump>)JsonFunctions.DeserializeFromStringToJson(App.ViewModel.settings.HeatpumpsSettings, typeof(ObservableCollection<Heatpump>));

            DataContext = App.ViewModel;

            // Get my SSID
            // SSID is used to detect if direct UDP communication could be used
            SSID = NetworkFunctions.FindWIFISSID();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

            // The name of our push channel.
            string channelName = "Heatpump_Channel";

            // Try to find the push channel.
            App.ViewModel.settings.pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (App.ViewModel.settings.pushChannel == null)
            {
                App.ViewModel.settings.pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                App.ViewModel.settings.pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                App.ViewModel.settings.pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Raw notification handler
                App.ViewModel.settings.pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);

                // Note that this is asynchronous - the channel exists only after ChannelUriUpdated fires
                App.ViewModel.settings.pushChannel.Open();

                // Bind this new channel for Tile and Toast events - not used yet
                // * Idea: Arduino could measure temperatures with DS18B20, and send notifications
                //   if the temperature gooes below an alarm limit (which again could be set by this app)
                //App.ViewModel.settings.pushChannel.BindToShellTile();
                //App.ViewModel.settings.pushChannel.BindToShellToast();
            }
            else
            {
                // The channel was already open, so just register for all the events.
                App.ViewModel.settings.pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                App.ViewModel.settings.pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Raw notification handler
                App.ViewModel.settings.pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine("Using existing push channel with URL: " + App.ViewModel.settings.pushChannel.ChannelUri.ToString());

                UIShortcuts();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Localize the appbar, this cannot be done with XAML bindings
            var appBarMenuItems = this.ApplicationBar.MenuItems as IList<IApplicationBarMenuItem>;
            appBarMenuItems[0].Text = AppResources.HeatpumpControllers;
            appBarMenuItems[1].Text = AppResources.ConnectionSettings;
            
            // Set the datacontext empty at first to force a refresh
            DataContext = null;
            DataContext = App.ViewModel;

            if (App.ViewModel.settings.pushChannel.ChannelUri != null)
            {
                UIShortcuts();
            }
        }

        // When the app is started for the first time, the ChannelUri exists only after this event has fired
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Display the new URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine("Notification channel URL updated to: " + e.ChannelUri.ToString());

                UIShortcuts();
            });
        }

        /// <summary>
        /// Event handler for when a Push Notification error occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        // Handles the Windows Phone RAW notification from the Arduino
        void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            string message;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body))
            {
                message = reader.ReadToEnd();
            }

            HeatPumpIdentifyResponse response = (HeatPumpIdentifyResponse)JsonFunctions.DeserializeFromStringToJson(message, typeof(HeatPumpIdentifyResponse));

            System.Diagnostics.Debug.WriteLine("response to command: " + response.command + ":\n" + message);

            if (App.ViewModel.notificationHandlers.ContainsKey(response.command))
            {
                System.Diagnostics.Debug.WriteLine("Found handler for command " + response.command);
                App.ViewModel.notificationHandlers[response.command](message);
            }
        }


        // UI shortcuts
        // * no pumps -> go to controller page
        // * one pump -> clear the navigation history and go directly to pump control
        private void UIShortcuts()
        {
            if (App.ViewModel.heatpumps.Count == 0)
            {
                if (SSID == null)
                {
                    MessageBox.Show(AppResources.NoHeatpumpsAndWiFi, AppResources.NoWiFi, MessageBoxButton.OK);

                    while (NavigationService.BackStack.Any())
                        NavigationService.RemoveBackEntry();
                    NavigationService.GoBack();
                }

                if (MessageBox.Show(AppResources.ConnectedToSSID + " '" + SSID + "'.", AppResources.SearchForControllers, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    App.ViewModel.settings.SSIDSetting = SSID;
                    this.NavigationService.Navigate(new Uri("/HeatpumpControllerPage.xaml", UriKind.Relative));
                }
            }
            else if (App.ViewModel.heatpumps.Count == 1)
            {
                this.NavigationService.Navigate(new Uri("/HeatpumpPage.xaml?heatpumpIndex=-1", UriKind.Relative));
            }
        }


        //
        // Navigate to the Settings page
        //
        private void Settings_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        //
        // Navigate to the Heatpump page
        //
        private void HeatpumpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox heatpumpList = sender as ListBox;

            if (heatpumpList.SelectedIndex == -1)
                return;

            this.NavigationService.Navigate(new Uri("/HeatpumpPage.xaml?heatpumpIndex=" + heatpumpList.SelectedIndex, UriKind.Relative));

            heatpumpList.SelectedIndex = -1;
        }

        //
        // Navigate to the Heatpump Controller page
        //
        private void Heatpumps_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HeatpumpControllerPage.xaml", UriKind.Relative));
        }

        //
        // When the list of heatpumps is reordered, save the reordered list
        //
        private void Heatpump_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            ReorderListBox.ReorderListBox heatpumpList = sender as ReorderListBox.ReorderListBox;

            Settings settings = App.ViewModel.settings;
            settings.HeatpumpsSettings = JsonFunctions.SerializeToJsonString(App.ViewModel.heatpumps);
            settings.Save();
        }
    }
}