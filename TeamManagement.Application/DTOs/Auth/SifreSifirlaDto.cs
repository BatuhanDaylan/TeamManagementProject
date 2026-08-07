namespace TeamManagement.Application.DTOs.Auth
{
    public class SifreSifirlamaTalepDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SifreSifirlaDto
    {
        public string Token { get; set; } = string.Empty;
        public string YeniSifre { get; set; } = string.Empty;
    }
}
