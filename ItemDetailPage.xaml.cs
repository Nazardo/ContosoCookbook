using ContosoCookbook.Common;
using ContosoCookbook.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace ContosoCookbook
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class ItemDetailPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private StorageFile _photo; // Photo file to share
        private StorageFile _video; // Video file to share

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public ItemDetailPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.SizeChanged += (s, e) => UpdateVisualState(e.NewSize.Width);
            // Register for DataRequested events
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequested;
        }

        private void UpdateVisualState(double width)
        {
            if (width < 500)
            {
                VisualStateManager.GoToState(this, "SmallPortrait", false);
            }
            else
            {
                VisualStateManager.GoToState(this, ApplicationView.GetForCurrentView().Orientation.ToString(), false);
            }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            RecipeDataItem item = await RecipeDataSource.GetItemAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;
        }

        private async void OnShootPhoto(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI camera = new CameraCaptureUI();
            StorageFile file = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (file != null)
            {
                _photo = file; DataTransferManager.ShowShareUI();
            }
        }

        private async void OnShootVideo(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI camera = new CameraCaptureUI();
            camera.VideoSettings.Format = CameraCaptureUIVideoFormat.Wmv;
            StorageFile file = await camera.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (file != null)
            {
                _video = file; DataTransferManager.ShowShareUI();
            }
        }

        private async void OnReminder(object sender, RoutedEventArgs e)
        {
            RecipeDataItem dataItem = contentRegion.DataContext as RecipeDataItem;
            ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
            // Make sure notifications are enabled
            if (notifier.Setting != NotificationSetting.Enabled)
            {
                MessageDialog dialog = new MessageDialog("Notifications are currently disabled");
                await dialog.ShowAsync();
                return;
            }
            // Get a toast template and insert a text node containing a message
            XmlDocument template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            IXmlNode element = template.GetElementsByTagName("text")[0];
            element.AppendChild(template.CreateTextNode("Reminder! Cook " + dataItem.Title));
            // Schedule the toast to appear 10 seconds from now
            DateTimeOffset date = DateTimeOffset.Now.AddSeconds(10);
            ScheduledToastNotification stn = new ScheduledToastNotification(template, date); notifier.AddToSchedule(stn);
        }

        private async void OnPinRecipe(object sender, RoutedEventArgs e)
        {
            RecipeDataItem item = (RecipeDataItem)contentRegion.DataContext;
            Uri uri = new Uri(item.TileImagePath);
            // first parameter is TileId, changed from item.UniqueId to a random Guid
            SecondaryTile tile = new SecondaryTile(new Guid().ToString(), item.Title, item.UniqueId, uri, TileSize.Square150x150);
            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            await tile.RequestCreateAsync();
        }

        void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            //RecipeDataItem item = (RecipeDataItem)this.flipView.SelectedItem;
            RecipeDataItem item = (RecipeDataItem)contentRegion.DataContext;
            request.Data.Properties.Title = item.Title;
            if (_photo != null)
            {
                request.Data.Properties.Description = "Recipe photo";
                RandomAccessStreamReference reference = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(_photo);
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
                _photo = null;
            }
            else if (_video != null)
            {
                request.Data.Properties.Description = "Recipe video";
                List<StorageFile> items = new List<StorageFile>();
                items.Add(_video);
                request.Data.SetStorageItems(items);
                _video = null;
            }
            else
            {
                request.Data.Properties.Description = "Recipe ingredients and directions";

                // Share recipe text
                string recipe = "\r\nINGREDIENTS\r\n";
                recipe += String.Join("\r\n", item.Ingredients);
                recipe += ("\r\n\r\nDIRECTIONS\r\n" + item.Directions);
                request.Data.SetText(recipe);

                // Share recipe image
                RandomAccessStreamReference reference = RandomAccessStreamReference.CreateFromUri(new Uri(item.ImagePath));
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
            }
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
            UpdateVisualState(Window.Current.Bounds.Width);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}