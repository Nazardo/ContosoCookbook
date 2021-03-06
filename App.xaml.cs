﻿using ContosoCookbook.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Grid App template is documented at http://go.microsoft.com/fwlink/?LinkId=234226

namespace ContosoCookbook
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                //Associate the frame with a SuspensionManager key                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(GroupedItemsPage), e.Arguments);
            }
            if (!String.IsNullOrEmpty(e.Arguments))
            {
                // If the app was activated from a secondary tile, show the recipe
                rootFrame.Navigate(typeof(ItemDetailPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            // Add commands to the settings pane
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;

            // Clear tiles and badges
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();

            // Register for push notifications if a connection is available
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                await RegisterForPushNotifications();
            }

            // Initialize CurrentAppSimulator for simulated app purchases
            StorageFile license = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DataModel/license.xml"));
            await CurrentAppSimulator.ReloadSimulatorAsync(license);
        }

        private async Task RegisterForPushNotifications()
        {
            // we need to keep track of the caught exception
            // because we cant' await on a message dialog in a catch block
            Exception error = null;
            try
            {
                PushNotificationChannel channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(channel.Uri, BinaryStringEncoding.Utf8);
                string uri = CryptographicBuffer.EncodeToBase64String(buffer);

                HttpClient client = new HttpClient();
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(5000);
                    // Wait up to 5 seconds
                    var response = await client.GetAsync(new Uri("http://ContosoRecipes8.cloudapp.net?uri=" + uri + "&type=tile")).AsTask(cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        await new MessageDialog("Unable to open push notification channel (request failed)").ShowAsync();
                    }
                }
                catch (OperationCanceledException timeOutException)
                {
                    error = timeOutException;
                }
                catch (Exception x)
                {
                    error = x;
                }
            }
            catch (Exception x)
            {
                // Probably running in the simulator
                error = x;
            }

            // handle errors if any
            if (error != null)
            {
                if (error is OperationCanceledException)
                {
                    await new MessageDialog("Unable to open push notification channel (operation timed out)").ShowAsync();
                }
                else
                {
                    await new MessageDialog("Unable to open push notification channel. Are you running in the simulator?").ShowAsync();
                }
            }
        }

        void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Add an About command to the settings pane
            SettingsCommand about = new SettingsCommand("about", "About", (handler) => new AboutSettingsFlyout().Show());
            args.Request.ApplicationCommands.Add(about);

            // Add a preferences command to the settings pane
            SettingsCommand preferences = new SettingsCommand("preferences", "Preferences", (handler) => new PreferencesSettingsFlyout().Show());
            args.Request.ApplicationCommands.Add(preferences);
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
