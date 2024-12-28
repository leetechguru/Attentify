using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbOrder
    {
        [Key]
        public long od_idx { get; set; }
        public long or_id { set; get; }
        public string or_name { set; get; }
        public Nullable<DateTime> or_date { set; get; }
        public string? or_customer { set; get; }
        public string? or_channel { set; get; }
        public Nullable<double> or_total { set; get; }
        public Nullable<int> or_payment_status{ set; get; }
        public Nullable<int> or_fulfill_status{ set; get; }
        public Nullable<int> or_itemCnt { set; get; }
        public Nullable<int> or_delivery_status { set; get; }
        public Nullable<int> or_delivery_method { set; get; }
        public string? or_tags { set; get; }
        public Nullable<int> or_status { set; get; }
        public string or_owner { set; get; }
        public string? or_phone { set; get; }
        public string? or_customer_name { set; get; }
    }
}
