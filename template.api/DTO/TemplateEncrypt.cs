namespace template.api.DTO
{
    public class TemplateEncrypt
    {
        public string CipherData { get; set; }
        public string Salt { get; set; }
        public string Iv { get; set; }
        public string CipherKey { get; set; }
    }
}