using Google.Cloud.PubSub.V1;
using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbShopifyUser
    {
        [Key]
        public long idx { get; set; }
        public string UserId { get; set; } // user email
        public string UserName { get; set; }
        public string UserShopifyDomain { set; get; }
        public string User_Id { get; set; } //customer id
        public Nullable<DateTime> createdAt { set; get; }
        public Nullable<DateTime> updatedAt { set; get; }
        public string? phone { set; get; }
        public string? address1 { set; get; }
        public string? address2 { set; get; }
        public string? city { set; get; }
        public string? province { set; get; }
        public string? country { set; get;}
        public string? province_code { set; get; }
        public string? zip { set; get; }
    }
}
