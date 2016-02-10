using Microsoft.OneDrive.Sdk;
using MorrisMusicStore.Common;
using MorrisMusicStore.Data;
using MorrisMusicStore.DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace MorrisMusicStore
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class SplitPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private DispatcherTimer _timer;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        private void MusicPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
           // throw new NotImplementedException();
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        MusicDataGroup group;
        public SplitPage()
        {
            this.InitializeComponent();

            // Setup the navigation helper
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Setup the logical page navigation components that allow
            // the page to only show one pane at a time.
            this.navigationHelper.GoBackCommand = new RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.itemListView.SelectionChanged += ItemListView_SelectionChanged;

            // Start listening for Window size changes 
            // to change from showing two panes to showing a single pane
            Window.Current.SizeChanged += Window_SizeChanged;
            this.InvalidateVisualState();

            this.Unloaded += SplitPage_Unloaded;

            MediaControl.PlayPressed += MediaControlPlayPressed;
            MediaControl.PausePressed += MediaControlPausePressed;
            MediaControl.PlayPauseTogglePressed += MediaControlPlayPauseTogglePressed;
            MediaControl.StopPressed += MediaControlStopPressed;
            MediaControl.FastForwardPressed += MediaControl_FastForwardPressed;
            MediaControl.RewindPressed += MediaControl_RewindPressed;
            MediaControl.ChannelDownPressed += MediaControl_ChannelDownPressed;
            MediaControl.ChannelUpPressed += MediaControl_ChannelUpPressed;

            myplayer.CurrentStateChanged += MyMediaCurrentStateChanged;

        }
        private async void MediaControlStopPressed(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => myplayer.Stop());
        }
        void MediaControl_ChannelUpPressed(object sender, object e)
        {
        }

        void MediaControl_ChannelDownPressed(object sender, object e)
        {
        }
        private async void MediaControlPausePressed(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => myplayer.Pause());
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            if (myplayer.CurrentState == MediaElementState.Playing)
            {
                myplayer.Pause();
            }
            else
            {
                myplayer.Play();
            }
        }

        void MyMediaCurrentStateChanged(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
            if (mediaElement == null)
                return;
            if (mediaElement.CurrentState == MediaElementState.Playing)
            {
                PlayButton.Icon = new SymbolIcon(Symbol.Pause);
                //PlayButton.Style = (Style)Application.Current.Resources["PauseAppBarButtonStyle"];
                StartTimer();
            }
            else if (mediaElement.CurrentState == MediaElementState.Paused ||
                     mediaElement.CurrentState == MediaElementState.Stopped)
            {
                PlayButton.Icon = new SymbolIcon(Symbol.Play);
                //PlayButton.Style = (Style)Application.Current.Resources["PlayAppBarButtonStyle"];
                 StopTimer();
            }
       //     else if( mediaElement.)
        }



        private async void MediaControlPlayPressed(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => myplayer.Play());
        }

        private async void MediaControlPlayPauseTogglePressed(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (myplayer.CurrentState == MediaElementState.Paused)
                {
                    myplayer.Play();
                }
                else
                {
                    myplayer.Pause();
                }
            });
        }


        void MediaControl_RewindPressed(object sender, object e)
        {
        }

        void MediaControl_FastForwardPressed(object sender, object e)
        {
        }


        //private async void btnPlayWavSound_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    MediaElement mysong = new MediaElement();
        //    Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
        //    Windows.Storage.StorageFile file = await folder.GetFileAsync("adios.wav");
        //    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
        //    mysong.SetSource(stream, file.ContentType);
        //    mysong.Play();
        //}

        /// <summary>
        /// Unhook from the SizedChanged event when the SplitPage is Unloaded.
        /// </summary>
        private void SplitPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Window_SizeChanged;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {


            ////  One drive authentication

            string clientId = "kZoNIQAeXdWUvxXcx2vyF1E72Rg03JRz";
            string returnUrl = "https://login.live.com/oauth20_authorize.srf";
            string[] scopes = { "onedrive.readwrite", "wl.offline_access", "wl.signin" };



            var oneDriveClient = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(scopes);

            await oneDriveClient.AuthenticateAsync();

            var item = await oneDriveClient
                     .Drive
                     .Root
                     .ItemWithPath("Music/test.mp3")
                     .Request()
                     .GetAsync();

            

            Debug.WriteLine(item.Audio.Duration);

            ////////



            group = await MusicDataSource.GetGroupAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;

            if (e.PageState == null)
            {
                this.itemListView.SelectedItem = null;
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (e.PageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    var selectedItem = await MusicDataSource.GetItemAsync((String)e.PageState["SelectedItem"]);
                    this.itemsViewSource.View.MoveCurrentTo(selectedItem);
                }
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="Common.SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="sender">The source of the event; typically <see cref="Common.NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = (Data.MusicDataItem)this.itemsViewSource.View.CurrentItem;
                if (selectedItem != null) e.PageState["SelectedItem"] = selectedItem.UniqueId;
            }
        }

        #region Logical page navigation

        // The split page is designed so that when the Window does have enough space to show
        // both the list and the details, only one pane will be shown at at time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        private const int MinimumWidthForSupportingTwoPanes = 768;

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <returns>True if the window should show act as one logical page, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation()
        {
            return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
        }

        /// <summary>
        /// Invoked with the Window changes size
        /// </summary>
        /// <param name="sender">The current Window</param>
        /// <param name="e">Event data that describes the new size of the Window</param>
        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
            
        }

        private bool CanGoBack()
        {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null)
            {
                return true;
            }
            else
            {
                return this.navigationHelper.CanGoBack();
            }
        }
        private void GoBack()
        {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                this.navigationHelper.GoBack();
            }
        }

        private void InvalidateVisualState()
        {
            var visualState = DetermineVisualState();
            VisualStateManager.GoToState(this, visualState, false);
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        private string DetermineVisualState()
        {
            if (!UsingLogicalPageNavigation())
                return "PrimaryView";

            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null;

            return logicalPageBack ? "SinglePane_Detail" : "SinglePane";
        }

        #endregion

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void SetupTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            StartTimer();
        }

        private void MyMediaMediaOpened1(object sender, RoutedEventArgs e)
        {
            SetupTimer();
        }

        private void TimerTick(object sender, object e)
        {
            audioTimeline.Text = myplayer.Position.ToString(@"mm\:ss");
        }

        private void StartTimer()
        {
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
            _timer.Tick -= TimerTick;
        }


        async void PlayMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                myplayer.Play();
            });
        }

        async void PauseMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                myplayer.Pause();
            });
        }
       
        private void myplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (itemListView.SelectedIndex == itemListView.Items.Count-1)
                itemListView.SelectedIndex = 0;
            else
                itemListView.SelectedIndex = ++itemListView.SelectedIndex;
            
        }

        private void myplayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            SetupTimer();
        }

        private void PrevButtonClick(object sender, RoutedEventArgs e)
        {
            if (itemListView.SelectedIndex == 0)
                itemListView.SelectedIndex = itemListView.Items.Count-1;
            else
                itemListView.SelectedIndex = --itemListView.SelectedIndex;

        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            if (itemListView.SelectedIndex == itemListView.Items.Count - 1)
                itemListView.SelectedIndex = 0;
            else
                itemListView.SelectedIndex = ++itemListView.SelectedIndex;

        }

        private void FavButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = (Data.MusicDataItem)this.itemsViewSource.View.CurrentItem;
            var ac = new PlaylistDataSource();
            //MusicDataGroup testObject = new MusicDataGroup("MyPlalist", "My Playlist", "Morris' Playlist", "", "This playlist contains favorite songs!");
            //ac.Groups = new MusicDataGroup("", "", "", "", "");
            var groups = new List<PlaylistDataGroup>();
            groups.Add(new PlaylistDataGroup("MyPlaylist", "My Playlist", "Player list", "", "This is my Playlist"));


            ac.Groups = new System.Collections.ObjectModel.ObservableCollection<PlaylistDataGroup>(groups);

                string x = JsonConvert.SerializeObject(ac);



            string y = selectedItem.Title;
        }
    }
}