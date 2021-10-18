using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamTopFtpWeb.Models
{
    public class RootFolder
    {
        public BlobFolder Root { get; set; }
        public List<BlobFolder> FlattenedFolders { get; set; }

        public RootFolder(BlobFolder root, List<BlobFolder> flattenedFolders)
        {
            Root = root;
            FlattenedFolders = flattenedFolders;
        }

        public BlobFolder GetFromRoot(string prefix)
        {
            prefix = prefix == null ? "" : prefix.EndsWith('/') ? prefix : prefix + "/";
            var testA = FlattenedFolders.Select(x => x.Prefix).ToList();

            var matchingFolder = FlattenedFolders.FirstOrDefault(x => x.Prefix == prefix);
            if (matchingFolder == null)
            {
                var trimmedPrefix = prefix.TrimEnd('/');
                trimmedPrefix = trimmedPrefix.Remove(trimmedPrefix.LastIndexOf('/')) + "/";
                matchingFolder = FlattenedFolders.FirstOrDefault(x => x.Prefix == trimmedPrefix);

                var file = matchingFolder.Files.Single(x => x.Name == prefix.TrimEnd('/'));

                var newFolder = new BlobFolder(matchingFolder);
                newFolder.Files.Add(file);
                return newFolder;
            }
            return matchingFolder;
        }
    }
}
