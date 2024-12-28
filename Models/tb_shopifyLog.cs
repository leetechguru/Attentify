using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbShopifyLog
    {
        [Key]
        public long idx { get; set; }
        public string UserId { get; set; } // User who performed the action
        public string Action { get; set; } // Description of the action (e.g., "Order Canceled")
        public int OrderId { get; set; } // Associated order ID
        public Nullable<DateTime> Timestamp { get; set; } // Date and time of the action
    }
}
