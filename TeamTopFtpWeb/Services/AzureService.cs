using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TeamTopFtpWeb.Models;

namespace TeamTopFtpWeb.Services
{
    public class AzureService : IAzureService
    {
        private const string CACHEKEY = "folderStructure";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureService> _logger;
        private IMemoryCache _cache;

        private CloudBlobClient _cloudBlobClient;
        private RootFolder _root;

        public AzureService(IConfiguration config, ILogger<AzureService> logger, IMemoryCache memoryCache)
        {
            _configuration = config;
            _logger = logger;
            _cache = memoryCache;
        }

        public async Task<BlobFolder> GetDataAsync(string prefix)
        {
            if (_cloudBlobClient == null)
            {
                Connect();
            }

            _root = await GetRootFromCacheOrBlobStorageAsync();

            return _root.GetFromRoot(prefix);
        }

        public BlobFolder GetData(string prefix)
        {
            if (_cloudBlobClient == null)
            {
                Connect();
            }

            _root = GetRootFromCacheOrBlobStorage();

            return _root.GetFromRoot(prefix);
        }

        public void ForceCacheRefresh()
        {
            _cache.Remove(CACHEKEY);
        }

        private RootFolder GetRootFromBlobStorage()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _logger.LogInformation("Connecting to Azure services and getting data..");

            var containerName = _configuration.GetValue<string>("AzureStorageAccount:BlobContainer");
            var container = _cloudBlobClient.GetContainerReference(containerName);
            var data = CreateTree(container, "");

            _logger.LogInformation($"Fetched data. Took {TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds)}");

            return new RootFolder(data, data.Flatten());
        }

        private async Task<RootFolder> GetRootFromBlobStorageAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _logger.LogInformation("Connecting to Azure services and getting data..");

            var containerName = _configuration.GetValue<string>("AzureStorageAccount:BlobContainer");
            var container = _cloudBlobClient.GetContainerReference(containerName);
            var data = await CreateTreeAsync(container, "");

            _logger.LogInformation($"Fetched data. Took {TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds)}");

            return new RootFolder(data, data.Flatten());
        }
        
        private BlobFolder CreateTree(CloudBlobContainer container, string prefix)
        {
            List<CloudBlockBlob> files = new List<CloudBlockBlob>();
            List<CloudBlobDirectory> subDirectories = container.ListBlobs(prefix)
                .Where(x => x is CloudBlobDirectory)
                .Cast<CloudBlobDirectory>()
                .ToList();

            var rootDirectory = new BlobFolder();
            rootDirectory.FolderName = prefix;
            rootDirectory.Prefix = prefix;
            rootDirectory.Files = files;
            foreach (CloudBlobDirectory directory in subDirectories)
            {
                var subDirectory = ParseTree(directory);
                subDirectory.FolderName = directory.Prefix;
                subDirectory.Prefix = directory.Prefix;
                rootDirectory.SubDirectories.Add(subDirectory);
            }

            return rootDirectory;
        }

        private async Task<BlobFolder> CreateTreeAsync(CloudBlobContainer container, string prefix)
        {
            List<CloudBlockBlob> files = new List<CloudBlockBlob>();
            List<CloudBlobDirectory> subDirectories = container.ListBlobs(prefix)
                .Where(x => x is CloudBlobDirectory)
                .Cast<CloudBlobDirectory>()
                .ToList();

            var rootDirectory = new BlobFolder();
            rootDirectory.FolderName = prefix;
            rootDirectory.Prefix = prefix;
            rootDirectory.Files = files;
            foreach (CloudBlobDirectory directory in subDirectories)
            {
                var subDirectory = await ParseTreeAsync(directory);
                subDirectory.FolderName = directory.Prefix;
                subDirectory.Prefix = directory.Prefix;
                rootDirectory.SubDirectories.Add(subDirectory);
            }

            return rootDirectory;
        }

        private BlobFolder ParseTree(CloudBlobDirectory blobDirectory)
        {
            var subDirectories = blobDirectory.ListBlobs().Where(x => x is CloudBlobDirectory).Cast<CloudBlobDirectory>().ToList();
            var subDirectoryNames = subDirectories.Select(x => {
                var directoryName = x.Prefix;
                return directoryName.Remove(directoryName.Length - 1);
            });
            var files = blobDirectory.ListBlobs()
                .Where(x => x is CloudBlockBlob
                && !subDirectoryNames.Contains(((CloudBlockBlob)x).Name))
                .Cast<CloudBlockBlob>()
                .ToList();

            files.ForEach(x =>
            {
                if (x.Name.EndsWith(".mp4") || x.Name.EndsWith(".MP4") && x.Properties.ContentType != "video/mp4")
                {
                    x.FetchAttributes();
                    x.Properties.ContentType = "video/mp4";
                    x.SetProperties();
                }
            });

            BlobFolder folder = new BlobFolder(blobDirectory);
            folder.FolderName = blobDirectory.Prefix;
            folder.Prefix = blobDirectory.Prefix;
            foreach (CloudBlobDirectory directory in subDirectories)
            {
                var subDirectory = ParseTree(directory);
                subDirectory.FolderName = directory.Prefix;
                subDirectory.Prefix = directory.Prefix;
                folder.SubDirectories.Add(subDirectory);
            }

            foreach (CloudBlockBlob file in files)
            {
                folder.Files.Add(file);
            }

            return folder;
        }

        private async Task<BlobFolder> ParseTreeAsync(CloudBlobDirectory blobDirectory)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await blobDirectory.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            // return results;

            var subDirectories = results.Where(x => x is CloudBlobDirectory).Cast<CloudBlobDirectory>().ToList();
            var subDirectoryNames = subDirectories.Select(x => {
                var directoryName = x.Prefix;
                return directoryName.Remove(directoryName.Length - 1);
            });
            var files = blobDirectory.ListBlobs()
                .Where(x => x is CloudBlockBlob
                && !subDirectoryNames.Contains(((CloudBlockBlob)x).Name))
                .Cast<CloudBlockBlob>()
                .ToList();

            files.ForEach(x =>
            {
                if (x.Name.EndsWith(".mp4") || x.Name.EndsWith(".MP4") && x.Properties.ContentType != "video/mp4")
                {
                    x.FetchAttributes();
                    x.Properties.ContentType = "video/mp4";
                    x.SetProperties();
                }
            });

            BlobFolder folder = new BlobFolder(blobDirectory);
            folder.FolderName = blobDirectory.Prefix;
            folder.Prefix = blobDirectory.Prefix;
            foreach (CloudBlobDirectory directory in subDirectories)
            {
                var subDirectory = ParseTree(directory);
                subDirectory.FolderName = directory.Prefix;
                subDirectory.Prefix = directory.Prefix;
                folder.SubDirectories.Add(subDirectory);
            }

            foreach (CloudBlockBlob file in files)
            {
                folder.Files.Add(file);
            }

            return folder;
        }

        private void Connect()
        {
            var storageConnectionString = _configuration.GetValue<string>("AzureStorageAccount:ConnectionString");

            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            }
            else
            {
                _logger.LogError("Couldn't parse connectionstring");
            }
        }

        private RootFolder GetRootFromCacheOrBlobStorage()
        {
            RootFolder cacheEntry;
            if (!_cache.TryGetValue(CACHEKEY, out cacheEntry))
            {
                cacheEntry = GetRootFromBlobStorage();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(10));

                _cache.Set(CACHEKEY, cacheEntry, cacheEntryOptions);
            }

            return cacheEntry;
        }

        private async Task<RootFolder> GetRootFromCacheOrBlobStorageAsync()
        {
            RootFolder cacheEntry;
            if (!_cache.TryGetValue(CACHEKEY, out cacheEntry))
            {
                cacheEntry = await GetRootFromBlobStorageAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(10));

                _cache.Set(CACHEKEY, cacheEntry, cacheEntryOptions);
            }

            return cacheEntry;
        }
    }
}
