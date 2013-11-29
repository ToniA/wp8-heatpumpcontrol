using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using Renci.SshNet;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Heatpump_Control
{

    public static class ModalUtils
    {
        private static Canvas myCanvas;
        private static ProgressIndicator myProgressIndicator;

        // The timeout timer
        public static DispatcherTimer timeoutTimer = new DispatcherTimer();
        

        public static void ShowProgressBar(DependencyObject DO, Grid LayoutRoot)
        {
            myCanvas = LayoutRoot.FindName("modal") as Canvas;
            myProgressIndicator = SystemTray.GetProgressIndicator(DO);

            myCanvas.Visibility = Visibility.Visible;
            myProgressIndicator.IsVisible = true;
        }

        public static void HideProgressBar()
        {
            myCanvas.Visibility = Visibility.Collapsed;
            myProgressIndicator.IsVisible = false;

        }
    }
}