using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbMailAccount
    {
        [Key]
        public long id { get; set; }
        public string mail { get; set; } 
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string accessToken { get; set; } // google account access token
        public string refreshToken { get; set; } // google account refresh token
    }
}
