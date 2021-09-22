using System;
using System.Text;
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
            var salt = RandomGenerator.Generate256BitsOfRandomEntropy();
            var iv = RandomGenerator.Generate256BitsOfRandomEntropy();
            string password = "1234";
            var salt64 = Convert.ToBase64String(salt);
            var iv64 = Convert.ToBase64String(iv);
            var encrypted = await Task.FromResult(RijndaelCipher.EncryptWithPassword(data.Data, password, salt64, iv64));

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            string passwordBase64 = Convert.ToBase64String(passwordBytes);
            var passPhrase = RsaCipher.Encrypt(passwordBase64, certificate);
            
            return Ok(new DTO.TemplateEncrypt{ Iv = iv64, CipherData = encrypted, Salt = salt64, CipherKey = passPhrase });
        }

        [AllowAnonymous]
        [HttpPost, Route("PostDecryptData")]
        public async Task<IActionResult> PostDecryptData([FromBody] DTO.TemplateEncrypt data)
        {
            var certificate = AuthenticationService.LoadCertificate(_config);
            var key = RsaCipher.Decrypt(data.CipherKey, certificate);
            var dataDecrypt = await Task.FromResult(RijndaelCipher.DecryptWithPassword(data.CipherData, key, data.Salt, data.Iv));
            return Ok(new DTO.Template{ Data = dataDecrypt });
        }
    }
}