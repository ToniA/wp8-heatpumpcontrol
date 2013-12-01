using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Notification;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Threading;
using Heatpump_Control.Resources;

namespace Heatpump_Control
{
    public partial class HeatpumpPage : PhoneApplicationPage
    {
        public HeatpumpPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            if (!App.ViewModel.rawNotificationHandlers.ContainsKey("command"))
            {
                // Raw notification handler for the 'command' response
                receiveCommandHandler commandHandler = handleCommandResponse;
                App.ViewModel.rawNotificationHandlers.Add("command", commandHandler);
            }
        }

        // Navigate to the requested pivot page, or first page requested page is '-1'
        // * '-1' means that there's only one pump, and the pump selection page is not shown at all
        // * in that case also the navigation history is cleaned, i.e. this page appears as the launch page
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Localize the appbar, this cannot be done with XAML bindings
            var appBarButtons = this.ApplicationBar.Buttons as IList<IApplicationBarIconButton>;
            appBarButtons[0].Text = AppResources.IRSend;

            var appBarMenuItems = this.ApplicationBar.MenuItems as IList<IApplicationBarMenuItem>;
            appBarMenuItems[0].Text = AppResources.HeatpumpControllers;
            appBarMenuItems[1].Text = AppResources.ConnectionSettings;

            // Navigate to the requested pivot page
            int heatpumpIndex = Int32.Parse(this.NavigationContext.QueryString["heatpumpIndex"]);

            if (heatpumpIndex == -1)
            {
                pivot.SelectedIndex = 0;

                while (NavigationService.BackStack.Any())
                    NavigationService.RemoveBackEntry();
            }
            else
            {
                pivot.SelectedIndex = heatpumpIndex;
            }
        }

        //
        // When a LoopingSelector is expanded, collapse the other selectors
        // * The other selectors are children of the same parent object in the GUI
        private void HandleSelectorIsExpandedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                // Collapse all LoopingSelectors except the one which sent the event
                CollapseLoopingSelectors(pivot, (DependencyObject)sender);
            }
        }

        // Collapse the loopingselectors also when the power state changes
        private void PowerState_Click(object sender, RoutedEventArgs e)
        {
            // Collapse all LoopingSelectors
            CollapseLoopingSelectors(pivot, null);
        }

        // Search all LoopingSelectors which are childred of the targetElement
        // * if the found LoopingSelector is not the 'thisElement', collapse it
        private void CollapseLoopingSelectors(DependencyObject targetElement, DependencyObject thisElement)
        {
            if (targetElement == thisElement)
                return;
            if (targetElement is Microsoft.Phone.Controls.Primitives.LoopingSelector)
            {
                ((Microsoft.Phone.Controls.Primitives.LoopingSelector)(targetElement)).IsExpanded = false;
                return;
            }

            int count = VisualTreeHelper.GetChildrenCount(targetElement);
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(targetElement, i);
                CollapseLoopingSelectors(child, thisElement);
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
        // Navigate to the Heatpump Controller page
        //
        private void Heatpumps_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HeatpumpControllerPage.xaml", UriKind.Relative));
        }

        // The 'send' command actions
        private void Send_Command(object sender, EventArgs e)
        {
            // Collapse all LoopingSelectors
            CollapseLoopingSelectors(pivot, null);

            // Save the heatpump state
            App.ViewModel.settings.HeatpumpsSettings = JsonFunctions.SerializeHeatpumps(App.ViewModel.heatpumps);
            App.ViewModel.settings.Save();

            // Show the progress bar and dim the app
            ModalUtils.ShowProgressBar((DependencyObject)this, LayoutRoot);

            // Create the command to send
            Heatpump selectedHeatpump = pivot.SelectedItem as Heatpump;
            HeatPumpStateCommand heatpumpCommand = new HeatPumpStateCommand();

            heatpumpCommand.command = "command";
            heatpumpCommand.identity = selectedHeatpump.controllerIdentity;
            heatpumpCommand.channel = App.ViewModel.settings.pushChannel.ChannelUri.Host + ":" +
                                      App.ViewModel.settings.pushChannel.ChannelUri.Port + ":" +
                                      App.ViewModel.settings.pushChannel.ChannelUri.AbsolutePath;
            heatpumpCommand.model = selectedHeatpump.heatpumpTypeName;

            heatpumpCommand.power = (selectedHeatpump.powerState == true) ? 1 : 0;
            heatpumpCommand.mode = (int)selectedHeatpump.operatingModes.SelectedItem;
            heatpumpCommand.fan = (int)selectedHeatpump.fanSpeeds.SelectedItem;
            heatpumpCommand.temperature = (int)selectedHeatpump.temperatures.SelectedItem;

            // Serialize the command to JSON and send it
            string Json = JsonFunctions.SerializeToJsonString(heatpumpCommand);
            System.Diagnostics.Debug.WriteLine(Json);
            NetworkFunctions.SendJson(Json);

            // Set the timeout for sending the command
            ModalUtils.timeoutTimer.Interval = TimeSpan.FromSeconds(15);
            ModalUtils.timeoutTimer.Tick += sendCommandTimeout;
            ModalUtils.timeoutTimer.Start();
        }

        // Handles the RAW notification from the Arduino
        // * Hide the progressbar
        // * Display the acknowledgement popup
        void handleCommandResponse(string response)
        {
            HeatPumpStateCommand cmdResponse = JsonFunctions.DeserializeStateCommandFromJsonString(response);

            Dispatcher.BeginInvoke(() =>
                {
                    ModalUtils.HideProgressBar();
                    ModalUtils.timeoutTimer.Stop();
                    ModalUtils.timeoutTimer.Tick -= sendCommandTimeout;

                    // The progressbar doesn't stop running without this
                    Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show(AppResources.CommandSent);
                    });
                }
            );
        }

        // If the timeout fires
        // * Hide the progressbar
        // * Display the 'no response received' popup
        private void sendCommandTimeout(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ModalUtils.timeoutTimer.Stop();
                ModalUtils.timeoutTimer.Tick -= sendCommandTimeout;
                ModalUtils.HideProgressBar();
                    // The progressbar doesn't stop running without this
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CommandNoResponse);
                });
            });
        }
    }
}