using Microsoft.Azure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;

namespace TeamTopFtpWeb.Models
{
    public class BlobFolder : CloudBlobDirectory
    {
        public CloudBlobDirectory Directory;
        public List<CloudBlockBlob> Files = new List<CloudBlockBlob>();
        public List<BlobFolder> SubDirectories = new List<BlobFolder>();
        public string FolderName;

        public new string Prefix { get; set; }

        public BlobFolder()
        {
        }

        public BlobFolder(CloudBlobDirectory blobDirectory)
        {
            Directory = blobDirectory;
        }

        public List<BlobFolder> Flatten()
        {
            var expandedSubDirs = SubDirectories.Expand(x => x.SubDirectories).ToList();       
            expandedSubDirs.Add(this);
            return expandedSubDirs;
        }
    }
}
