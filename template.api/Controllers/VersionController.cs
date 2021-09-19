using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace workspace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return Content(version, "text/plain", Encoding.UTF8);
        }
    }
}
