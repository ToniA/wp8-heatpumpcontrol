using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;
using Heatpump_Control.Resources;

namespace Heatpump_Control
{
    public partial class HeatpumpControllerPage : PhoneApplicationPage
    {
        public HeatpumpControllerPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            if (!App.ViewModel.rawNotificationHandlers.ContainsKey("identify"))
            {
                // Raw notification handler for the 'identify' response
                receiveCommandHandler identifyHandler = handleIdentifyResponse;
                App.ViewModel.rawNotificationHandlers.Add("identify", identifyHandler);
            }
        }

        // Check if any new controller can be found
        // * If not, take the shortcut action to identify immediately
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Localize the appbar, this cannot be done with XAML bindings
            var appBarButtons = this.ApplicationBar.Buttons as IList<IApplicationBarIconButton>;
            appBarButtons[0].Text = AppResources.Save;

            var appBarMenuItems = this.ApplicationBar.MenuItems as IList<IApplicationBarMenuItem>;
            appBarMenuItems[0].Text = AppResources.SearchControllers;

            
            if (App.ViewModel.heatpumps.Count == 0)
            {
                Send_Identify();
            }
            else if (App.ViewModel.heatpumps.Count == 1)
            {
                App.ViewModel.heatpumps[0].expanded = true;
            }

        }

        // Expand the selected controller on the list
        private void ControllerList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TextBlock controllerId = sender as TextBlock;

            foreach (Heatpump heatpump in App.ViewModel.heatpumps)
            {
                if (heatpump.controllerIdentity.Equals(controllerId.Text))
                {
                    heatpump.expanded = !heatpump.expanded;
                }
            }
        }


        // When the heatpumps are saved
        // * Serialize the data and save it to isolated storage
        // * As a shortcut action, go back one page
        private void SaveHeatpumps_Click(object sender, EventArgs e)
        {
            Settings settings = App.ViewModel.settings;

            foreach (Heatpump heatpump in App.ViewModel.heatpumps)
            {
                heatpump.expanded = false;
            }

            settings.HeatpumpsSettings = JsonFunctions.SerializeHeatpumps(App.ViewModel.heatpumps);
            settings.Save();

            NavigationService.GoBack();
        }

        // How to search for controllers
        private void SearchControllers_Click(object sender, EventArgs e)
        {
            Send_Identify();
        }

        //
        // Send the 'identify yourself' broadcast to the heatpump controller
        //
        private void Send_Identify()
        {
            System.Diagnostics.Debug.WriteLine("sending identify command");

            // Show the progress bar and dim the app
            ModalUtils.ShowProgressBar((DependencyObject)this, LayoutRoot);

            // Create the command to send
            HeatPumpIdentifyCommand identifyCommand = new HeatPumpIdentifyCommand();

            identifyCommand.command = "identify";
            identifyCommand.channel = App.ViewModel.settings.pushChannel.ChannelUri.Host + ":" +
                                      App.ViewModel.settings.pushChannel.ChannelUri.Port + ":" +
                                      App.ViewModel.settings.pushChannel.ChannelUri.AbsolutePath;

            // Serialize the command to JSON and send it
            string Json = JsonFunctions.SerializeToJsonString(identifyCommand);
            System.Diagnostics.Debug.WriteLine(Json);
            NetworkFunctions.SendJson(Json);

            // Set the timeout for sending the command
            ModalUtils.timeoutTimer.Interval = TimeSpan.FromSeconds(15);
            ModalUtils.timeoutTimer.Tick += sendIdentifyTimeout;
            ModalUtils.timeoutTimer.Start();
        }

        // Handles the RAW notification from the Arduino
        // * Hide the progressbar
        // * Add the new controllers into the list of found heatpump controllers
        // * There might be multiple responses if there are multiple controllers, the first one will hide the progressbar
        void handleIdentifyResponse(string response)
        {
            HeatPumpStateCommand identifyResponse = JsonFunctions.DeserializeStateCommandFromJsonString(response);

            Heatpump foundController = new Heatpump(identifyResponse.identity);

            Dispatcher.BeginInvoke(() =>
            {
                if (!App.ViewModel.heatpumps.Any(p => p.controllerIdentity.Equals(foundController.controllerIdentity)))
                {
                    App.ViewModel.heatpumps.Add(foundController);
                }
                
                ModalUtils.HideProgressBar();
                ModalUtils.timeoutTimer.Stop();
                ModalUtils.timeoutTimer.Tick -= sendIdentifyTimeout;
            }
            );
        }


        // If the timeout fires, hide the modal progressbar and display an error message
        private void sendIdentifyTimeout(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ModalUtils.timeoutTimer.Stop();
                ModalUtils.timeoutTimer.Tick -= sendIdentifyTimeout;
                ModalUtils.HideProgressBar();

                // The progressbar doesn't stop running without this
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.ControllersNotFound);
                });
            });
        }
    }
}