using Gw2_GuildEmblem_Cdn.Controllers;
using Gw2_GuildEmblem_Cdn.Model.Statistics;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class StatisticsUtility
    {
        private const int DISK_JOB_INTERVAL = 60 /*sec*/ * 1000 /*ms*/;
        public const string STATISTICS_DIRECTORY_NAME = "stat";
        public const string ZIP_NAME = "stats.zip";
        private const string NO_REFERRER_NAME = "none";

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Regex _guidRegex = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}", RegexOptions.Compiled);
        private static Regex _numericRegex = new Regex(@"^(0|[1-9][0-9]*)$", RegexOptions.Compiled);

        private static Lazy<StatisticsUtility> _instance = new Lazy<StatisticsUtility>(() => new StatisticsUtility());
        public static StatisticsUtility Instance
        {
            get => _instance.Value;
        }

        private bool _runDiskJob;
        private object _containerLock = new object();
        private object _zipLock = new object();
        private string _currentFile;
        private StatisticsContainer _container;


        public StatisticsUtility()
        {
            _runDiskJob = true;
            _container = new StatisticsContainer();

            LoadFromDisk(GetCurrentDiskStateFilePath());
            RunDiskJob();
        }

        #region State saving to disk

        /// <summary>
        /// Starts the Thread that periodically saves the stat state to disk
        /// </summary>
        private void RunDiskJob()
        {
            Thread thread = new Thread(() =>
            {
                while (_runDiskJob)
                {
                    DiskJob();
                    Thread.Sleep(DISK_JOB_INTERVAL);
                }
            });

            thread.Start();
        }


        /// <summary>
        /// Saves the stat state to disk
        /// </summary>
        private void DiskJob()
        {
            try
            {
                string filePath = GetCurrentDiskStateFilePath();

                if (!System.IO.File.Exists(filePath))
                {
                    if (!string.IsNullOrEmpty(_currentFile))
                    {
                        //Current file does not exist
                        // -> new day, while a file was previously written
                        // -> Serialize current state to old day file, start a new one

                        SaveStateToDisk(_currentFile);
                        AddToZip(_currentFile);
                        System.IO.File.Delete(_currentFile);
                        _container = new StatisticsContainer();
                    }
                    _currentFile = filePath;
                }

                SaveStateToDisk(_currentFile);
                AddToZip(_currentFile);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Archives Stat jsons
        /// </summary>
        /// <param name="filePath"></param>
        private void AddToZip(string filePath)
        {
            string entryFileName = Path.GetFileName(filePath);
            string zipPath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], ZIP_NAME);
            lock (_zipLock)
            {
                using (FileStream zipStream = new FileStream(zipPath, FileMode.OpenOrCreate))
                using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = zipArchive.GetEntry(entryFileName);
                    if (entry != null)
                        entry.Delete();

                    zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Optimal);
                }
            }
        }

        /// <summary>
        /// Returns the file path for the current state file
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentDiskStateFilePath()
        {
            return Path.Combine(ConfigurationManager.AppSettings["cache_path"], STATISTICS_DIRECTORY_NAME, $"{DateTime.UtcNow.ToString("yyyy.MM.dd")}.json");
        }


        private void SaveStateToDisk(string filePath)
        {
            lock (_containerLock)
            {
                string json = JsonConvert.SerializeObject(_container, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json, Encoding.UTF8);
            }
        }

        private void LoadFromDisk(string filePath)
        {
            lock (_containerLock)
            {
                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
                    _container = JsonConvert.DeserializeObject<StatisticsContainer>(json);
                    _currentFile = filePath;
                }
            }
        }

        /// <summary>
        /// Returns a number of Containers from the zip
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<(string, StatisticsContainer)> GetFromZip(uint count)
        {
            List<(string, StatisticsContainer)> lstEntries = new List<(string, StatisticsContainer)>();

            string zipPath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], ZIP_NAME);
            lock (_zipLock)
            {
                using (FileStream zipStream = new FileStream(zipPath, FileMode.Open))
                using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    List<ZipArchiveEntry> archiveEntries = zipArchive.Entries.OrderByDescending(x => x.LastWriteTime).Take(Convert.ToInt32(count)).ToList();
                    foreach(ZipArchiveEntry entry in archiveEntries)
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            string json = reader.ReadToEnd();
                            StatisticsContainer container = JsonConvert.DeserializeObject<StatisticsContainer>(json);
                            lstEntries.Add((entry.Name, container));

                            reader.Close();
                        }
                    }
                }
            }

            return lstEntries;
        }

        #endregion

        /// <summary>
        /// Registers the Creation of an emblem
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        public void RegisterCreationAsync(Guild guild, int size)
        {
            ThreadHelper.Run(() => RegisterCreation(guild, size));
        }

        /// <summary>
        /// Registers the Creation of an emblem
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        private void RegisterCreation(Guild guild, int size)
        {
            string id;
            try
            {
                if (guild != null && guild.Emblem != null)
                {
                    string flags = guild.Emblem.Flags.Count() > 0 ? $"_{string.Join(".", guild.Emblem.Flags.Select(x => x.Value))}" : string.Empty;
                    id = $"{guild.Emblem.Background.Id}-{string.Join(".", guild.Emblem.Background.Colors)}_{guild.Emblem.Foreground.Id}-{string.Join(".", guild.Emblem.Foreground.Colors)}{flags}_{size}";
                }
                else
                {
                    id = $"null_{size}";
                }

                lock (_containerLock)
                {
                    if (!_container.EmblemCreations.ContainsKey(id))
                    {
                        _container.EmblemCreations.Add(id, new EmblemCreationContainer()
                        {
                            CreatedAt = DateTime.UtcNow,
                            CreatedByGuild = guild.Id,
                            Emblem = guild.Emblem,
                            Size = size
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers a Response by its Request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cached"></param>
        public void RegisterResponseAsync(HttpRequestMessage request, bool cached)
        {
            ThreadHelper.Run(() => RegisterResponse(request, cached));
        }

        /// <summary>
        /// Registers a Response by its Request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cached"></param>
        private void RegisterResponse(HttpRequestMessage request, bool cached)
        {
            try
            {
                if (_guidRegex.IsMatch(request.RequestUri.AbsoluteUri))
                {
                    Guid guid;
                    int size;

                    string penultimateSegment = request.RequestUri.Segments.Reverse().Skip(1).First();
                    string lastSegment = request.RequestUri.Segments.Reverse().First();

                    //Check position of requested Guild-ID
                    // -> last = number, penultimate = Guid: Custom size
                    // -> last = Guid: Default size
                    Match guidMatchPenultimateSegment = _guidRegex.Match(penultimateSegment);
                    Match sizeMatchLastSegment = _numericRegex.Match(lastSegment);
                    Match guidMatchLastSegment = _guidRegex.Match(lastSegment);

                    if (
                        guidMatchPenultimateSegment.Success &&
                        sizeMatchLastSegment.Success &&
                        Guid.TryParse(guidMatchPenultimateSegment.Value, out guid) &&
                        int.TryParse(sizeMatchLastSegment.Value, out size))
                    {
                        //Request with a specified size
                        RegisterResponse(guid, size, cached);
                    }
                    else if (
                        !sizeMatchLastSegment.Success &&
                        guidMatchLastSegment.Success &&
                        Guid.TryParse(guidMatchLastSegment.Value, out guid))
                    {
                        //Request with default size
                        RegisterResponse(guid, EmblemController.DEFAULT_IMAGE_SIZE, cached);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers a Response by its Request
        /// </summary>
        /// <param name="requestedGuildId"></param>
        /// <param name="size"></param>
        /// <param name="servedFromCache"></param>
        public void RegisterResponseAsync(Guid requestedGuildId, int size, bool servedFromCache)
        {
            ThreadHelper.Run(() => RegisterResponse(requestedGuildId, size, servedFromCache));
        }

        /// <summary>
        /// Registers a Response by its Request
        /// </summary>
        /// <param name="requestedGuildId"></param>
        /// <param name="size"></param>
        /// <param name="servedFromCache"></param>
        private void RegisterResponse(Guid requestedGuildId, int size, bool servedFromCache)
        {
            try
            {
                lock (_containerLock)
                {
                    string key = $"{requestedGuildId}_{size}_{servedFromCache}";
                    int hour = DateTime.UtcNow.Hour;

                    if (!_container.Responses.ContainsKey(hour))
                    {
                        _container.Responses.Add(hour, new Dictionary<string, ResponseContainer>());
                    }

                    if (!_container.Responses[hour].ContainsKey(key))
                    {
                        _container.Responses[hour].Add(key, new ResponseContainer()
                        {
                            GuildId = requestedGuildId,
                            ServedFromCache = servedFromCache,
                            Size = size
                        });
                    }
                    _container.Responses[hour][key].Count++;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers an API Endpoint call and wether it was served from cache or not
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="servedFromCache"></param>
        public void RegisterApiEndpointCallAsync(string endpoint, bool servedFromCache)
        {
            ThreadHelper.Run(() => RegisterApiEndpointCall(endpoint, servedFromCache));
        }

        /// <summary>
        /// Registers an API Endpoint call and wether it was served from cache or not
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="servedFromCache"></param>
        private void RegisterApiEndpointCall(string endpoint, bool servedFromCache)
        {
            try
            {
                lock (_containerLock)
                {
                    string key = $"{endpoint}_{servedFromCache}";
                    int hour = DateTime.UtcNow.Hour;

                    if (!_container.ApiEndpointCalls.ContainsKey(hour))
                    {
                        _container.ApiEndpointCalls.Add(hour, new Dictionary<string, ApiEndpointCallContainer>());
                    }

                    if (!_container.ApiEndpointCalls[hour].ContainsKey(key))
                    {
                        _container.ApiEndpointCalls[hour].Add(key, new ApiEndpointCallContainer()
                        {
                            Endpoint = endpoint,
                            ServedFromCache = servedFromCache
                        });
                    }
                    _container.ApiEndpointCalls[hour][key].Count++;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers the exceedance of the Ratelimiter Limit
        /// </summary>
        public void RegisterRatelimitExceedanceAsync()
        {
            ThreadHelper.Run(() => RegisterRatelimitExceedance());
        }

        /// <summary>
        /// Registers the exceedance of the Ratelimiter Limit
        /// </summary>
        private void RegisterRatelimitExceedance()
        {
            try
            {
                lock (_containerLock)
                {
                    int hour = DateTime.UtcNow.Hour;

                    if (!_container.RatelimitExceedances.ContainsKey(hour))
                    {
                        _container.RatelimitExceedances.Add(hour, 0);
                    }

                    _container.RatelimitExceedances[hour]++;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers a referrer
        /// </summary>
        /// <param name="request"></param>
        public void RegisterReferrerAsync(HttpRequestMessage request, bool servedFromCache)
        {
            ThreadHelper.Run(() => RegisterReferrer(request, servedFromCache));
        }

        /// <summary>
        /// Registers a referrer
        /// </summary>
        /// <param name="request"></param>
        private void RegisterReferrer(HttpRequestMessage request, bool servedFromCache)
        {
            try
            {
                if (request.Headers.Referrer != null)
                {
                    RegisterReferrerInternal(request.Headers.Referrer.AbsoluteUri, servedFromCache);
                }
                else
                {
                    RegisterReferrerInternal(null, servedFromCache);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Registers a referrer
        /// </summary>
        /// <param name="referrer"></param>
        private void RegisterReferrerInternal(string referrer, bool servedFromCache)
        {
            lock (_containerLock)
            {
                int hour = DateTime.UtcNow.Hour;
                string referrerBase = NO_REFERRER_NAME;

                if (!string.IsNullOrWhiteSpace(referrer))
                {
                    referrerBase = new Uri(referrer).Host;
                }

                if (!_container.Referrers.ContainsKey(hour))
                {
                    _container.Referrers.Add(hour, new Dictionary<string, Dictionary<bool, int>>());
                }

                if (!_container.Referrers[hour].ContainsKey(referrerBase))
                {
                    _container.Referrers[hour].Add(referrerBase, new Dictionary<bool, int>());
                }

                if (!_container.Referrers[hour][referrerBase].ContainsKey(servedFromCache))
                {
                    _container.Referrers[hour][referrerBase].Add(servedFromCache, 0);
                }

                _container.Referrers[hour][referrerBase][servedFromCache]++;
            }
        }

        /// <summary>
        /// Registers an emblem request by its descriptor string
        /// </summary>
        /// <param name="descriptor"></param>
        public void RegisterEmblemRequestAsync(string descriptor)
        {
            ThreadHelper.Run(() => RegisterEmblemRequest(descriptor));
        }

        private void RegisterEmblemRequest(string descriptor)
        {
            lock(_containerLock)
            {
                int hour = DateTime.UtcNow.Hour;
                if(!_container.CreatedEmblemsRequests.ContainsKey(hour))
                {
                    _container.CreatedEmblemsRequests.Add(hour, new Dictionary<string, int>());
                }

                if (!_container.CreatedEmblemsRequests[hour].ContainsKey(descriptor))
                {
                    _container.CreatedEmblemsRequests[hour].Add(descriptor, 0);
                }

                _container.CreatedEmblemsRequests[hour][descriptor]++;
            }
        }
    }
}