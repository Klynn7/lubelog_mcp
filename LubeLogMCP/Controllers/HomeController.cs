using LubeLogMCP.Helper;
using LubeLogMCP.Models;
using Microsoft.AspNetCore.Mvc;

namespace LubeLogMCP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LubeLoggerApiClient _api;

        public HomeController(ILogger<HomeController> logger, LubeLoggerApiClient api)
        {
            _logger = logger;
            _api = api;
        }
        public async Task<IActionResult> Index()
        {
            //perform health check
            var viewModel = new HealthVM();
            if (!string.IsNullOrWhiteSpace(_api.Instance))
            {
                viewModel.Instance = _api.Instance;
                // The MCP holds no credentials of its own, so this only checks that LubeLogger responds;
                // a 401 from /api/version still means the instance is up.
                var response = await _api.GetAsync("/api/version");
                if (response.TransportError != null)
                {
                    _logger.LogError("LubeLogger health check failed: {Error}", response.TransportError);
                }
                viewModel.IsConnected = response.TransportError == null;
            }
            else
            {
                viewModel.Instance = "Not Configured";
            }
            return Json(viewModel);
        }
    }
}
