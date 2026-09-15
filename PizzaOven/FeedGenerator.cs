using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        Sounds,
        Tutorials,
        Collections
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

        public static Dictionary<string, GameBananaCollectionList> collectionFeed;
        public static bool error;
        public static Exception exception;
        public static GameBananaModList CurrentFeed;

        public static GameBananaCollectionList CollectionCurrentFeed;
        public static int? CollectionID = null;

        public static string? CollectionFileID = null;

        private sealed class CachedGameBananaRecord
        {
            public GameBananaRecord Record { get; init; }
            public DateTime CachedAt { get; init; }
        }

        private static readonly Dictionary<string, CachedGameBananaRecord> gameBananaRecordCache = new();
        private static readonly TimeSpan gameBananaRecordCacheDuration = TimeSpan.FromMinutes(5);
        private const int MaxGameBananaRecordCacheEntries = 100;

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

            lock (gameBananaRecordCache)
                gameBananaRecordCache.Clear();
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

                },

                Game = new GameBananaGame
                {
                    Name = "Pizza Tower"
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

        public class LocalCollectionData
        {
            public Dictionary<string, string> Metadata { get; set; } = new();
            public List<string> Items { get; set; } = new();
        }

        public static LocalCollectionData ParseLocalCollection(string collectionName)
        {
            var collectionpath = $@"{Global.assemblyLocation}{Global.s}LocalCollections{Global.s}{collectionName}";
            var collectioninfo = $@"{collectionpath}{Global.s}info.txt";

            LocalCollectionData data = new LocalCollectionData();

            if (!File.Exists(collectioninfo))
                return data;

            string section = "";

            foreach (string line in File.ReadAllLines(collectioninfo))
            {
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    section = trimmed[1..^1];
                    continue;
                }

                if (section == "METADATA")
                {
                    string[] parts = trimmed.Split('=', 2);

                    if (parts.Length == 2)
                        data.Metadata[parts[0].Trim()] = parts[1].Trim();
                }
                else if (section == "ITEMS")
                {
                    data.Items.Add(trimmed);
                }
            }

            return data;
        }

        public static bool TryParseLocalCollection(string collectionName, out LocalCollectionData data)
        {
            data = null;

            try
            {
                string collectionPath = $@"{Global.assemblyLocation}{Global.s}LocalCollections{Global.s}{collectionName}";
                string infoPath = $@"{collectionPath}{Global.s}info.txt";

                if (!File.Exists(infoPath))
                    return false;

                data = ParseLocalCollection(collectionName);
                return true;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        public static async Task<GameBananaModList> MakeLocalCollectionRecord(string collectionName, int page = 1, int perPage = 15)
        {
            LocalCollectionData data = ParseLocalCollection(collectionName);

            ObservableCollection<GameBananaRecord> records = new ObservableCollection<GameBananaRecord>();

            int startIndex = (page - 1) * perPage;
            int endIndex = Math.Min(startIndex + perPage, data.Items.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                GameBananaRecord record = await MakeGBlinktoRecord(data.Items[i]);

                if (record != null)
                    records.Add(record);
            }

            return new GameBananaModList
            {
                Records = records,
                TotalPages = data.Items.Count > 0 ? (int)Math.Ceiling((double)data.Items.Count / perPage) : 1,
                TimeFetched = DateTime.UtcNow
            };
        }

        public static async Task<GameBananaModList> MakeGBTut(string gameID, int page, int perPage, string? search, FeedFilter filter)
        {
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();

            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);

            using (var httpClient = new HttpClient())
            {
                var filtercondition = "";
                switch (filter)
                {
                    case FeedFilter.Recent:
                        filtercondition += "&_sOrder=updated";
                        break;
                    case FeedFilter.Featured:
                        filtercondition += "&_aArgs[]=_bWasFeatured = true& _sOrder=updated";
                        break;
                    case FeedFilter.Popular:
                        filtercondition += "&_sOrderBy=_nViewCount";
                        break;
                }
                var requestUrl = $"https://gamebanana.com/apiv11/Tutorial/Index?_aFilters[Generic_Game]={gameID}{filtercondition}&_nPage={page}&_nPerpage={perPage}";
                if (search != null)
                    requestUrl += $"&_sName={FixString(search)}";

                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                {
                    return feed[requestUrl];

                }
                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var root = JsonSerializer.Deserialize<JsonElement>(json);

                var metadata = root.GetProperty("_aMetadata");
                var recordCount = metadata.GetProperty("_nRecordCount").GetInt32();

                var records = new ObservableCollection<GameBananaRecord>();
                foreach (var record in root.GetProperty("_aRecords").EnumerateArray())
                {
                    var recordurl = record.GetProperty("_sProfileUrl").GetString();
                    var resultrecord = await MakeGBlinktoRecord(recordurl);

                    if (resultrecord != null)
                    {
                        records.Add(resultrecord);
                    }
                }

                var resultrecordslist = new GameBananaModList
                {
                    Records = records,
                    TotalPages = (int)Math.Ceiling((double)recordCount / perPage),
                    TimeFetched = DateTime.UtcNow
                };

                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, resultrecordslist);
                else
                    feed[requestUrl] = resultrecordslist;

                return resultrecordslist;
            }
        }

        public static async Task<GameBananaCollectionList> MakeCollection(string gameID, int page, int perPage, string? search)
        {
            if (collectionFeed == null)
                collectionFeed = new Dictionary<string, GameBananaCollectionList>();

            if (collectionFeed.Count > 15)
                collectionFeed.Remove(collectionFeed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);

            using (var httpClient = new HttpClient())
            {
                var requestUrl = $"https://gamebanana.com/apiv11/Collection/Index?_aFilters[Generic_Game]={gameID}&_sOrder=updated&_nPage={page}&_nPerpage={perPage}";

                if (search != null)
                    requestUrl += $"&_sName={FixString(search)}";

                if (collectionFeed.ContainsKey(requestUrl) && collectionFeed[requestUrl].IsValid)
                {
                    return collectionFeed[requestUrl];
                }

                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var root = JsonSerializer.Deserialize<JsonElement>(json);

                var metadata = root.GetProperty("_aMetadata");
                var recordCount = metadata.GetProperty("_nRecordCount").GetInt32();

                var collections = new ObservableCollection<GameBananaCollection>();

                foreach (var record in root.GetProperty("_aRecords").EnumerateArray())
                {
                    var collection = JsonSerializer.Deserialize<GameBananaCollection>(record.GetRawText());

                    if (collection != null)
                    {
                        try
                        {
                            string collectionhompageURL = $"https://gamebanana.com/apiv11/Collection/{collection.Id}/Items?_nPage=1&_nPerpage=15";

                            var collectionResponse = await httpClient.GetAsync(collectionhompageURL);
                            collectionResponse.EnsureSuccessStatusCode();

                            var collectionJson = await collectionResponse.Content.ReadAsStringAsync();
                            var collectionRoot = JsonSerializer.Deserialize<JsonElement>(collectionJson);

                            var collectionMetadata = collectionRoot.GetProperty("_aMetadata");

                            collection.Records = collectionMetadata.GetProperty("_nRecordCount").GetInt32();

                            collections.Add(collection);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                var GBCollectionList = new GameBananaCollectionList
                {
                    Records = collections,
                    TotalPages = (int)Math.Ceiling((double)recordCount / perPage),
                    TimeFetched = DateTime.UtcNow
                };

                if (!collectionFeed.ContainsKey(requestUrl))
                    collectionFeed.Add(requestUrl, GBCollectionList);
                else
                    collectionFeed[requestUrl] = GBCollectionList;

                return GBCollectionList;
            }
        }
        public static async Task<GameBananaModList>MakeGBlinkstoModList(List<string> links, int pages = 15)
        {
            var records = new ObservableCollection<GameBananaRecord>();
            foreach (var link in links)
            {
                GameBananaRecord record = await MakeGBlinktoRecord(link);
                if (record != null)
                    records.Add(record);
            }
            var returnrecord = new GameBananaModList
            {
                Records = records,
                TotalPages = (int)Math.Ceiling((double)records.Count / pages),
                TimeFetched = DateTime.UtcNow
            };
            return returnrecord;
        }

        public static async Task<GameBananaRecord?> MakeGBlinktoRecord(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return null;

            string cacheKey = link.Trim();
            lock (gameBananaRecordCache)
            {
                if (gameBananaRecordCache.TryGetValue(cacheKey, out CachedGameBananaRecord cachedRecord))
                {
                    if (DateTime.UtcNow - cachedRecord.CachedAt < gameBananaRecordCacheDuration)
                        return cachedRecord.Record;

                    gameBananaRecordCache.Remove(cacheKey);
                }
            }

            using (var httpClient = new HttpClient())
            {
                if (link.StartsWith("https://gamebanana.com/"))
                {
                    link = link.Replace("https://gamebanana.com/", "");
                }
                else
                {
                    return null;
                }

                string[] data = link.Split('/');

                string itemType = data[0] switch
                {
                    "mods" => "Mod",
                    "wips" => "Wip",
                    "tools" => "Tool",
                    "tuts" => "Tutorial",
                    _ => ""
                };

                string itemId = data.Length > 1 ? data[1] : "";

                if (itemType == "" || itemId == "")
                    return null;

                List<string> fields = new List<string>
                {
                    "name",
                    "description",
                    "text",
                    "views",
                    "likes",
                    "downloads",
                    "date",
                    "mdate",
                    "Nsfw().bIsNsfw()",
                    "userid",
                    "Category().name",
                    "catid",
                    "RootCategory().id",
                    "RootCategory().name",
                    "Files().aFiles()",
                    "screenshots",
                    "Game().name"
                };

                if (itemType == "Tutorial")
                {
                    fields.Remove("Files().aFiles()");
                    fields.Remove("downloads");
                }

                string apiUrl =
                    $"https://api.gamebanana.com/Core/Item/Data?itemtype={itemType}&itemid={itemId}&fields=";

                for (int i = 0; i < fields.Count; i++)
                {
                    apiUrl += fields[i];

                    if (i < fields.Count - 1)
                        apiUrl += ",";
                }

                apiUrl += "&return_keys=1";

                string profileApiUrl = $"https://gamebanana.com/apiv10/{itemType}/{itemId}/ProfilePage";

                string profileJson = await httpClient.GetStringAsync(profileApiUrl);

                using JsonDocument profileDocument = JsonDocument.Parse(profileJson);

                JsonElement profileRoot = profileDocument.RootElement;

                string json = await httpClient.GetStringAsync(apiUrl);

                Dictionary<string, JsonElement> dataDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                var files = new List<GameBananaItemFile>();

                if (dataDictionary.ContainsKey("Files().aFiles()") && itemType != "Tool" && itemType != "Tutorial")
                {
                    if (dataDictionary["Files().aFiles()"].ValueKind == JsonValueKind.Object)
                    {
                        foreach (var file in dataDictionary["Files().aFiles()"].EnumerateObject())
                        {
                            var fileData = file.Value;

                            try
                            {
                                files.Add(new GameBananaItemFile
                                {
                                    Id = fileData.GetProperty("_idRow").GetString(),
                                    FileName = fileData.GetProperty("_sFile").GetString(),
                                    Filesize = fileData.GetProperty("_nFilesize").GetInt64(),
                                    DownloadUrl = fileData.GetProperty("_sDownloadUrl").GetString(),
                                    Description = fileData.GetProperty("_sDescription").GetString(),
                                    ContainsExe = false,
                                    Downloads = fileData.GetProperty("_nDownloadCount").GetInt32(),
                                    DateAddedLong = fileData.GetProperty("_tsDateAdded").GetInt64()
                                });
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        files = null;
                    }
                }
                else
                {
                    files = null;
                }

                var media = new List<GameBananaImage>();
                try
                {
                    var screenshots = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(dataDictionary["screenshots"].GetString());

                    foreach (var screenshot in screenshots)
                    {
                        var mediaModelType = itemType.ToLowerInvariant() switch
                        {
                            "tutorial" => "tuts",
                            "mod" => "mods",
                            "wip" => "wips",
                            "tool" => "tools",
                            _ => null
                        };

                        if (mediaModelType == null)
                            continue;

                        media.Add(new GameBananaImage
                        {
                            Type = "image",
                            Base = new Uri($"https://images.gamebanana.com/img/ss/{mediaModelType}/"),
                            File = new Uri(screenshot["_sFile"].GetString(), UriKind.Relative),
                            Caption = screenshot["_sCaption"].GetString()
                        });
                    }
                }
                catch { }

                Uri CategoryIcon = new Uri("about:blank");
                Uri rootCategoryIcon = new Uri("about:blank");

                string CategoryName = " ";
                string rootCategoryName = " ";

                if (profileRoot.TryGetProperty("_aCategory", out JsonElement category))
                {
                    if (category.TryGetProperty("_sName", out JsonElement name))
                    {
                        CategoryName = name.GetString();
                    }
                    if (category.TryGetProperty("_sIconUrl", out JsonElement icon))
                    {
                        CategoryIcon = string.IsNullOrEmpty(icon.GetString()) ? new Uri("about:blank") : new Uri(icon.GetString());
                    }
                }

                if (profileRoot.TryGetProperty("_aSuperCategory", out JsonElement rootCategory))
                {
                    if (rootCategory.TryGetProperty("_sName", out JsonElement name))
                    {
                        rootCategoryName = name.GetString();
                    }
                    if (rootCategory.TryGetProperty("_sIconUrl", out JsonElement icon))
                    {
                        rootCategoryIcon = string.IsNullOrEmpty(icon.GetString()) ? new Uri("about:blank") : new Uri(icon.GetString());
                    }
                }

                List<GameBananaAlternateFileSource> profileAlternateFiles = null;

                if (itemType != "Tool" && itemType != "Tutorial")
                {
                    if (profileRoot.TryGetProperty("_aAlternateFileSources", out JsonElement alternateFiles))
                    {
                        profileAlternateFiles = JsonSerializer.Deserialize<List<GameBananaAlternateFileSource>>(alternateFiles.GetRawText());
                    }
                    else
                    {
                        profileAlternateFiles = null;
                    }
                }

                try
                {
                    var newrecord = new GameBananaRecord();

                    newrecord.Link = new Uri(profileRoot.GetProperty("_sProfileUrl").GetString());
                    newrecord.Title = profileRoot.GetProperty("_sName").GetString();
                    newrecord.Description = profileRoot.TryGetProperty("_sDescription", out var description) ? description.GetString() ?? "" : "";
                    newrecord.Text = profileRoot.TryGetProperty("_sText", out var text) ? text.GetString() ?? "" : "";
                    newrecord.Views = profileRoot.TryGetProperty("_nViewCount", out var views) ? views.GetInt32() : 0;
                    newrecord.Likes = profileRoot.TryGetProperty("_nLikeCount", out var likes) ? likes.GetInt32() : 0;
                    newrecord.Downloads = profileRoot.TryGetProperty("_nDownloadCount", out var downloads) && itemType != "Tutorial" ? downloads.GetInt32() : 0;
                    newrecord.DateAddedLong = profileRoot.TryGetProperty("_tsDateAdded", out var dateAdded) ? dateAdded.GetInt64() : 0;
                    newrecord.DateUpdatedLong = profileRoot.TryGetProperty("_tsDateUpdated", out var dateUpdated) ? dateUpdated.GetInt64() : 0;
                    newrecord.IsNsfw = dataDictionary.ContainsKey("Nsfw().bIsNsfw()") && dataDictionary["Nsfw().bIsNsfw()"].ValueKind != JsonValueKind.Null ? dataDictionary["Nsfw().bIsNsfw()"].GetBoolean() : false;

                    newrecord.Owner = JsonSerializer.Deserialize<GameBananaMember>(profileRoot.GetProperty("_aSubmitter").GetRawText());

                    newrecord.Category = new GameBananaCategory();
                    newrecord.Category.Name = CategoryName;
                    newrecord.Category.Icon = CategoryIcon;

                    newrecord.RootCategory = new GameBananaCategory();
                    newrecord.RootCategory.Name = rootCategoryName;
                    newrecord.RootCategory.Icon = rootCategoryIcon;

                    newrecord.AllFiles = files;
                    newrecord.Media = media;
                    newrecord.AlternateFileSources = profileAlternateFiles;

                    newrecord.Game = new GameBananaGame();
                    newrecord.Game.Name = dataDictionary["Game().name"].GetString();

                    if (itemType == "Tool")
                    {
                        newrecord.ForceCompatibleFalse = true;
                        newrecord.NotCompatibleString = "Can't Download Tools";
                    }

                    lock (gameBananaRecordCache)
                    {
                        if (gameBananaRecordCache.Count >= MaxGameBananaRecordCacheEntries)
                        {
                            string oldestKey = gameBananaRecordCache
                                .OrderBy(entry => entry.Value.CachedAt)
                                .First().Key;
                            gameBananaRecordCache.Remove(oldestKey);
                        }

                        gameBananaRecordCache[cacheKey] = new CachedGameBananaRecord
                        {
                            Record = newrecord,
                            CachedAt = DateTime.UtcNow
                        };
                    }

                    return newrecord;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static async Task<GameBananaModList> MakeCollectionToRecords(int id, int page, bool nsfw = false, string search = null, FeedFilter filter = FeedFilter.Recent)
        {
            
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();

            if (feed.Count > 15)
                feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);

            using (var httpClient = new HttpClient())
            {
                string apiUrl = $"https://gamebanana.com/apiv11/Collection/{id}/Items?_nPage={page}&_nPerpage=15";

                if (feed.ContainsKey(apiUrl) && feed[apiUrl].IsValid)
                {
                    return feed[apiUrl];
                }

                if (search != null)
                    apiUrl += $"&_sName={FixString(search)}";

                if (!nsfw)
                    apiUrl += "&_aArgs[]=_bHasContentRatings = false";

                switch (filter)
                {
                    case FeedFilter.Recent:
                        apiUrl += "&_sOrderBy=_tsDateUpdated";
                        break;
                    case FeedFilter.Featured:
                        apiUrl += "&_aArgs[]=_bWasFeatured = true& _sOrderBy=_tsDateAdded";
                        break;
                    case FeedFilter.Popular:
                        apiUrl += "&_sOrderBy=_nViewCount";
                        break;
                }

                string json = await httpClient.GetStringAsync(apiUrl);

                GameBananaCollectionInfo collectionInfo = JsonSerializer.Deserialize<GameBananaCollectionInfo>(json);

                var records = new ObservableCollection<GameBananaRecord>();

                foreach (GameBananaCollectionInfoRecords collectionRecord in collectionInfo.Records)
                {
                    GameBananaRecord record = await MakeGBlinktoRecord(collectionRecord.Link.ToString());

                    if (record != null)
                        records.Add(record);
                }

                var recordcount = collectionInfo.Metadata.RecordCount;
                var returnrecord = new GameBananaModList
                {
                    Records = records,
                    TotalPages = Math.Max(1, (int)Math.Ceiling(recordcount / (double)collectionInfo.Metadata.PerPage)),
                    TimeFetched = DateTime.UtcNow
                };

                if (!feed.ContainsKey(apiUrl))
                    feed.Add(apiUrl, returnrecord);
                else
                    feed[apiUrl] = returnrecord;
                return returnrecord;
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
