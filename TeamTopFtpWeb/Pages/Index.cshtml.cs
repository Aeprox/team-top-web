using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using TeamTopFtpWeb.Models;
using TeamTopFtpWeb.Services;

namespace TeamTopFtpWeb.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public BlobFolder FileStructure { get; private set; }

        private readonly ILogger<IndexModel> _logger;
        private readonly IAzureService _azureService;

        public IndexModel(ILogger<IndexModel> logger,
            IAzureService azureService)
        {
            _logger = logger;
            _azureService = azureService;
        }

        public async Task<IActionResult> OnGetAsync(string url)
        {
            FileStructure = await _azureService.GetDataAsync(url);
            return Page();
        }
    }
}
