using MorrisMusicStore.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace MorrisMusicStore.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class MusicDataItem
    {
        public MusicDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        
        public BitmapImage ArtWork
        {
            get
            {
                var img =  @ImagePath.ToString(); 
                var imgBytes = Convert.FromBase64String(img);
                var ms = new InMemoryRandomAccessStream();
                var dw = new Windows.Storage.Streams.DataWriter(ms);
                dw.WriteBytes(imgBytes);
                dw.StoreAsync();
                ms.Seek(0);

                var bm = new BitmapImage();
                bm.SetSource(ms);
                return bm;
            }
        }

    
        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class MusicDataGroup
    {
        public MusicDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<MusicDataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<MusicDataItem> Items { get; private set; }

        public BitmapImage ArtWork
        {
            get
            {
                var img = @ImagePath.ToString();
                var imgBytes = Convert.FromBase64String(img);
                var ms = new InMemoryRandomAccessStream();
                var dw = new Windows.Storage.Streams.DataWriter(ms);
                dw.WriteBytes(imgBytes);
                dw.StoreAsync();
                ms.Seek(0);

                var bm = new BitmapImage();
                bm.SetSource(ms);
                return bm;
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// MusicDataSource initializes with data read from a static json file from the 
    /// server.  This provides data at both design-time and run-time.
    /// </summary>
    public sealed class MusicDataSource
    {
        private static MusicDataSource _musicDataSource = new MusicDataSource();

        private ObservableCollection<MusicDataGroup> _groups = new ObservableCollection<MusicDataGroup>();
        public ObservableCollection<MusicDataGroup> Groups
        {
            get { return this._groups; }
            set { }
        }

        public static async Task<IEnumerable<MusicDataGroup>> GetGroupsAsync()
        {
            
            try {
                await _musicDataSource.GetMusicDataAsync();
                return _musicDataSource.Groups;
            }catch(HttpRequestException ex)
            {
                Debug.WriteLine(ex.InnerException.Message);
                //ex.Message;
                return null;
            }
        }

        public static async Task<MusicDataGroup> GetGroupAsync(string uniqueId)
        {
            await _musicDataSource.GetMusicDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _musicDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<MusicDataItem> GetItemAsync(string uniqueId)
        {
            await _musicDataSource.GetMusicDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _musicDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }


        static async void dotest(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine(response.StatusCode.ToString());
                }
                else
                {
                    // problems handling here
                    Debug.WriteLine(
                        "Error occurred, the status code is: {0}",
                        response.StatusCode
                    );
                }
            }
        }


        private async Task GetMusicDataAsync()
        {
            string responseString = "";
           // string path = "http://www.shahmalav.com/api/tracks.json";
            string path = "http://76.228.72.40/tracks.json";
            dotest(path);

            if (this._groups.Count != 0)
                return;

            Uri uri = new Uri(path);

            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                responseString = await response.Content.ReadAsStringAsync();
            }
            
            /// test using local storage file
            /*
             StorageFile file = await StorageFile
             StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
             string jsonText = await FileIO.ReadTextAsync(file);
             */

            string jsonText = responseString;
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                MusicDataGroup group = new MusicDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new MusicDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Content"].GetString()));
                }
                this.Groups.Add(group);
            }
        }
    }
}