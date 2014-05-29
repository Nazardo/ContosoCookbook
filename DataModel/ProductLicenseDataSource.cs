using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store;
using Windows.UI.Core;

namespace ContosoCookbook
{
    class ProductLicenseDataSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const string _requiresProductLicenseGroupTitleName = "Italian";
        private string _price;
        private string _groupTitle;

        public ProductLicenseDataSource()
        {
            CurrentAppSimulator.LicenseInformation.LicenseChanged += OnLicenseChanged;
        }

        public string GroupTitle
        {
            set
            {
                _groupTitle = value;

                //-- Group Title impacts all these...
                PropertyChanged(this, new PropertyChangedEventArgs("IsProductLicensed"));
                PropertyChanged(this, new PropertyChangedEventArgs("IsLicensed"));
                PropertyChanged(this, new PropertyChangedEventArgs("IsTrial"));
                PropertyChanged(this, new PropertyChangedEventArgs("ShowProductPurchaseButton"));
                PropertyChanged(this, new PropertyChangedEventArgs("GroupTitle"));
            }
            get
            {
                return _groupTitle;
            }
        }

        private async void GetListingInformationAsync()
        {
            ListingInformation listing = await CurrentAppSimulator.LoadListingInformationAsync();
            _price = listing.ProductListings[_requiresProductLicenseGroupTitleName].FormattedPrice;
        }

        private async void OnLicenseChanged()
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            //Fire PropertyChanged events on the UI thread
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsLicensed"));
                    PropertyChanged(this, new PropertyChangedEventArgs("IsProductLicensed"));
                    PropertyChanged(this, new PropertyChangedEventArgs("IsTrial"));
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowProductPurchaseButton"));
                }
            });
        }

        public bool IsProductLicensed
        {
            get
            {
                if (GroupTitle == null)
                    return false;

                if (GroupTitle != _requiresProductLicenseGroupTitleName)
                    return true;

                if (IsTrial)
                    return false;

                var licenses = CurrentAppSimulator.LicenseInformation.ProductLicenses;
                if (licenses == null || licenses[_requiresProductLicenseGroupTitleName] == null)
                    return false;

                return licenses[_requiresProductLicenseGroupTitleName].IsActive;
            }
        }

        public bool ShowProductPurchaseButton
        {
            get
            {
                return !IsProductLicensed;
            }
        }

        public bool IsLicensed
        {
            get
            {
                return CurrentAppSimulator.LicenseInformation.IsActive;
            }
        }

        public bool IsTrial
        {
            get { return CurrentAppSimulator.LicenseInformation.IsTrial; }
        }

        public string FormattedPrice
        {
            get
            {
                if (!String.IsNullOrEmpty(_price))
                    return "Purchase Italian Recipes for " + _price;
                else
                    return "Purchase Italian Recipes";
            }
        }

        public async void PurchaseProduct(string productName)
        {
            PurchaseResults results = await CurrentAppSimulator.RequestProductPurchaseAsync(productName);
            if (results.Status == ProductPurchaseStatus.Succeeded)
                OnLicenseChanged();
        }
    }
}
