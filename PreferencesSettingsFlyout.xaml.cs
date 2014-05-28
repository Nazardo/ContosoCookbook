using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ContosoCookbook
{
    public sealed partial class PreferencesSettingsFlyout : SettingsFlyout
    {
        private const string _key = "UseLocalDataSource";
        public PreferencesSettingsFlyout()
        {
            this.InitializeComponent();
            // Initialize the ToggleSwitch control

            //// Local Settings
            //if (ApplicationData.Current.LocalSettings.Values.ContainsKey(_key))
            //{
            //    DataSwitch.IsOn = !(bool)ApplicationData.Current.LocalSettings.Values[_key];
            //}

            // Roaming Settings
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey(_key))
            {
                DataSwitch.IsOn = !(bool)ApplicationData.Current.RoamingSettings.Values[_key];
            }
        }

        private void OnToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            //ApplicationData.Current.LocalSettings.Values[_key] = !DataSwitch.IsOn;
            ApplicationData.Current.RoamingSettings.Values[_key] = !DataSwitch.IsOn;
        }
    }
}
