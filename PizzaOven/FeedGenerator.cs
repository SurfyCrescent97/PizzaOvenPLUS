using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace PizzaOven
{
    public enum GameFilter
    {
        DBFZ,
        MHOJ2,
        GBVS,
        GGS,
        JF,
        KHIII,
        SN,
        ToA,
        DS,
        IM,
        SMTV,
        KOFXV,
        DNF
    }
    public enum FeedFilter
    {
        Featured,
        Recent,
        Popular,
        None
    }
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds
    }
    public static class FeedGenerator
    {

        private static HttpListener? _listener;
        private static string? _currentTempPath;

        private static async Task<string> MakeRonnieMod()
        {
            if (_listener?.IsListening == true)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
            }

            if (_currentTempPath != null && File.Exists(_currentTempPath))
            {
                try { File.Delete(_currentTempPath); }
                catch { }
            }

            string packUri = "pack://application:,,,/PizzaOven;component/TutorialMod/RonnieMod.zip";

            var resource = Application.GetResourceStream(new Uri(packUri));
            if (resource == null)
                return null;

            _currentTempPath = Path.Combine(Path.GetTempPath(), "RonnieMod.zip");

            using (var fileStream = new FileStream(_currentTempPath, FileMode.Create, FileAccess.Write))
            {
                resource.Stream.CopyTo(fileStream);
            }

            resource.Stream.Close();

            string url = "http://localhost:5000/RonnieMod.zip";

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5000/");
            _listener.Start();

            _ = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();

                        byte[] fileBytes = File.ReadAllBytes(_currentTempPath);

                        context.Response.ContentType = "application/zip";
                        context.Response.ContentLength64 = fileBytes.Length;
                        await context.Response.OutputStream.WriteAsync(fileBytes);
                        context.Response.Close();
                    }
                    catch
                    {
                        break;
                    }
                }
            });

            return url;
        }

        public static Dictionary<string, GameBananaModList> feed;
        public static bool error;
        public static Exception exception;
        public static GameBananaModList CurrentFeed;
        public static double GetHeader(this HttpResponseMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return -1;
            return Double.Parse(keys.First());
        }
        public static void ClearCache()
        {
            if (feed != null)
                feed.Clear();
        }

        public static async Task GetFakeFeed(int page, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search)
        {
            error = false;

            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();

            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);



            var fakeRecord = new GameBananaRecord
            {
                Title = "Ronnie Oven Mod",
                Description = "Our Favorite Oven",
                Text = "<h1>Ronnie Mod</h1>This never before seen mod is made for my favorite superhero Ronnie the Oven!<br><br>if you don't know who Ronnie is, what the hell man, he's talking to you RIGHT NOW!<br><br>oh yeah! you Get to play as Ronnie the Oven!! wow!! Moveset: you can double jump, you break if you run into a wall and your groundpound initiates a nuke!!<br><br>Man I sure hope this mod works very well I put a lot of effort into it I also hope Ronnie sees this he's my superstar",
                Views = 0,
                Likes = -5,
                Downloads = 1,
                DateAddedLong = DateTimeOffset.UtcNow.AddDays(-5).ToUnixTimeSeconds(),
                DateUpdatedLong = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                IsNsfw = false,

                Owner = new GameBananaMember
                {
                    Name = "SurfyCrescent97",
                    Avatar = new Uri("pack://application:,,,/PizzaOven;component/TutorialMod/profile.png", UriKind.Absolute),
                    Upic = new Uri("pack://application:,,,/PizzaOven;component/TutorialMod/upic.gif", UriKind.Absolute)
                },

                Category = new GameBananaCategory
                {
                    Name = "",
                    Icon = new Uri("pack://application:,,,/PizzaOven;component/TutorialMod/category.jpg", UriKind.Absolute)
                },

                RootCategory = new GameBananaCategory
                {
                    Name = "Full Game Edit",
                    Icon = new Uri("pack://application:,,,/PizzaOven;component/TutorialMod/category.jpg", UriKind.Absolute)
                },

                AllFiles = new List<GameBananaItemFile>
                {
                    new GameBananaItemFile
                    {
                        Id = "file1",
                        FileName = "ronnie_mod_v1.zip",
                        Filesize = 1024 * 932,
                        DownloadUrl = await MakeRonnieMod(),
                        Description = "Main mod file",
                        ContainsExe = false,
                        Downloads = 0,
                        DateAddedLong = DateTimeOffset.UtcNow.AddDays(-5).ToUnixTimeSeconds()
                    }
                },

                Media = new List<GameBananaImage>
                {
                    new GameBananaImage
                    {
                        Type = "image",
                        Base = new Uri("pack://application:,,,/PizzaOven;component/TutorialMod", UriKind.Absolute),
                        File = new Uri("mod.png", UriKind.Relative),
                        Caption = "Our Oven Ronnie!"
                    }
                },

                AlternateFileSources = new List<GameBananaAlternateFileSource>
                {

                }
            };
            CurrentFeed = new GameBananaModList
            {
                Records = new ObservableCollection<GameBananaRecord> { fakeRecord },
                TotalPages = 1,
                TimeFetched = DateTime.UtcNow
            };

            var fakeKey = $"fake_{page}_{category?.Name}_{subcategory?.Name}";
            if (!feed.ContainsKey(fakeKey))
                feed.Add(fakeKey, CurrentFeed);
            else
                feed[fakeKey] = CurrentFeed;

            await Task.CompletedTask;
        }

        //UNUSED FOR NOW
        public static async Task GetCollection(string gameID, int perPage)
        {
            using (var httpClient = new HttpClient())
            {
                var requestUrl = $"https://gamebanana.com/apiv11/Collection/Index?_aFilters[Generic_Game]={gameID}&_sOrder=updated&_nPage=1&_nPerpage={perPage}";
                var response = await httpClient.GetAsync(requestUrl);
                var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
            }
        }
        private static string FixString(string input = "")
        {
            if (input == null)
               return "";
            return input
                .Replace("\\", "\\\\") 
                .Replace("\"", "\\\"")  
                .Replace("'", "\\'");   
        }
        public static async Task GetFeed(int page, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search)
        {
            error = false;
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
            // Remove oldest key if more than 15 pages are cached
            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);
            using (var httpClient = new HttpClient())
            {
                if (!string.IsNullOrEmpty(search))
                    search = FixString(search);
                var requestUrl = GenerateUrl(page, type, filter, category, subcategory, perPage, nsfw, search);
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                {
                    CurrentFeed = feed[requestUrl];
                    return;
                }
                CurrentFeed = new();
                try
                {
                    var response = await httpClient.GetAsync(requestUrl);
                    var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(await response.Content.ReadAsStringAsync());
                    CurrentFeed.Records = records;
                    // Get record count from header
                    var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
                    if (numRecords != -1)
                    {
                        var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                        if (totalPages == 0)
                            totalPages = 1;
                        CurrentFeed.TotalPages = totalPages;
                    }
                }
                catch (Exception e)
                {
                    error = true;
                    exception = e;
                    return;
                }
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, CurrentFeed);
                else
                    feed[requestUrl] = CurrentFeed;
            }
        }
        private static string GenerateUrl(int page, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search)
        {
            // Base
            var url = "https://gamebanana.com/apiv6/";
            switch (type)
            {
                case TypeFilter.Mods:
                    url += "Mod/";
                    break;
                case TypeFilter.Sounds:
                    url += "Sound/";
                    break;
                case TypeFilter.WiPs:
                    url += "Wip/";
                    break;
            }
            // Different starting endpoint if requesting all mods instead of specific category
            if (search != null)
                url += $"ByName?_sName=*{search}*&_idGameRow=7692&";
            else if (category.ID != null)
                url += "ByCategory?";
            else
                url += $"ByGame?_aGameRowIds[]=7692&";
            // Consistent args
            url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
                $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources&_nPerpage={perPage}";
            if (!nsfw)
                url += "&_aArgs[]=_sbIsNsfw = false";
            // Sorting filter
            switch (filter)
            {
                case FeedFilter.Recent:
                    url += "&_sOrderBy=_tsDateUpdated,DESC";
                    break;
                case FeedFilter.Featured:
                    url += "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC";
                    break;
                case FeedFilter.Popular:
                    url += "&_sOrderBy=_nDownloadCount,DESC";
                    break;
            }
            // Choose subcategory or category
            if (subcategory.ID != null)
                url += $"&_aCategoryRowIds[]={subcategory.ID}";
            else if (category.ID != null)
                url += $"&_aCategoryRowIds[]={category.ID}";
            
            // Get page number
            url += $"&_nPage={page}";
            return url;
        }
    }
}
