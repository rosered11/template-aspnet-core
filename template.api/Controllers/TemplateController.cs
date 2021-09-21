using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace template.api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]s")]
    [ApiController]
    [ApiVersion("1")]
    [Authorize]
    public class TemplateController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] DTO.Template data)
        {
            return Ok(await Task.FromResult(data));
        }
    }
}