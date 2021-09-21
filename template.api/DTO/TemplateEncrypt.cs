namespace template.api.DTO
{
    public class TemplateEncrypt
    {
        public string base64Data { get; set; }
        public string salt { get; set; }
        public string iv { get; set; }
    }
}