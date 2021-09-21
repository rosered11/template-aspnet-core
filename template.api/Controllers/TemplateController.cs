using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using template.domain.Utilities;

namespace template.api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]s")]
    [ApiController]
    [ApiVersion("1")]
    [Authorize]
    public class TemplateController : Controller
    {
        private readonly IConfiguration _config;
        public TemplateController(IConfiguration config)
        {
            _config = config;
        }
        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] DTO.Template data)
        {
            return Ok(await Task.FromResult(data));
        }

        [AllowAnonymous]
        [HttpPost, Route("PostEncryptData")]
        public async Task<IActionResult> PostEncryptData([FromBody] DTO.Template data)
        {
            var certificate = AuthenticationService.LoadCertificate(_config);
            var base64Data = AesCipher.Encrypt(data.Data, "1234", out byte[] salt, out byte[] iv);
            Console.WriteLine($"Aes encrypt: ==> {base64Data}");
            
            var saltText = Convert.ToBase64String(salt);
            var ivText = Convert.ToBase64String(iv);

            var a = AesCipher.Decrypt(base64Data, "1234", saltText, ivText);
            Console.WriteLine($"Aes decript: ==> {a}");

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost, Route("PostDecryptData")]
        public async Task<IActionResult> PostDecryptData([FromBody] DTO.TemplateEncrypt data)
        {
            var certificate = AuthenticationService.LoadCertificate(_config);
            var dataDecrypt = await Task.FromResult(RsaCipher.Decrypt(data.base64Data, certificate));
            return Ok(new DTO.Template{ Data = dataDecrypt });
        }
    }
}