using Signal_Windows.ViewModels;
using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Signal_Windows.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class AddContactPage : Page
    {
        public AddContactPage()
        {
            this.InitializeComponent();
            Vm.View = this;
        }

        public AddContactPageViewModel Vm
        {
            get
            {
                return (AddContactPageViewModel)DataContext;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs ev)
        {
            base.OnNavigatedTo(ev);

            Utils.EnableBackButton(Vm.BackButton_Click);

            try
            {
                await Vm.OnNavigatedTo(); // causes Exception when try Add Contact (maybe *)
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] AddContact -OnNavigatedTo- Exception: " + ex.Message);
            }
            /*
             * I don't have the same environment ( i also target mobile and running April update )

                Just added Microsoft.Toolkit.Uwp.UI 3.0 to my project and had the exact same error

                Downgrading to 2.2 give the following error :
                Cannot find type Windows.UI.Xaml.Controls.NavigationView in module Windows.Foundation.UniversalApiContract.winmd.
                Downgrading to 2.1.1 build fine :/
            */
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Utils.DisableBackButton(Vm.BackButton_Click);
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await Vm.AddButton_Click(sender, e);
            Frame.Navigate(typeof(MainPage));
        }

        private async void ContactsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Vm.ContactsList_ItemClick(sender, e);
            Frame.Navigate(typeof(MainPage));
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            Vm.SearchBox_TextChanged(sender, args);
        }

        private void ContactNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Vm.ContactNameTextBox_TextChanged(sender, e);
        }

        private void ContactNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Vm.ContactNumberTextBox_TextChanged(sender, e);
        }

        private async void ContactsList_RefreshRequested(object sender, System.EventArgs e)
        {
            await Vm.RefreshContacts();
        }
    }
}