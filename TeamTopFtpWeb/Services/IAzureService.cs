using System.Threading.Tasks;
using TeamTopFtpWeb.Models;

namespace TeamTopFtpWeb.Services
{
    public interface IAzureService
    {
        BlobFolder GetData(string url);
        Task<BlobFolder> GetDataAsync(string prefix);
        void ForceCacheRefresh();
    }
}
