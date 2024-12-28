using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbShopifyToken
    {
        [Key]
        public long idx { get; set; }
        public int Id { get; set; }
        public string? UserId { get; set; } // Links to the authenticated user in your system
        public string AccessToken { get; set; } // Shopify API access token
        public string ShopDomain { get; set; } // The Shopify store domain (e.g., "example.myshopify.com")
        public Nullable<DateTime> DateCreated { get; set; }
        public Nullable<DateTime> DateUpdated { get; set; }
    }
}
