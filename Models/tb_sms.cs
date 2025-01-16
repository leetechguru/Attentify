using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbSms
    {
        [Key]
        public long sm_idx { get; set; }
        public string sm_id { set; get; }
        public string sm_to { set; get; }
        public string sm_body { set; get; }
        public string sm_from { set; get;}
        public Nullable<DateTime> sm_date { set; get; }
        public Nullable<int> sm_read { set; get; }
        public Nullable<int> sm_state { set; get; }

        public TbSms()
        {
            sm_id   = string.Empty;
            sm_to   = string.Empty;
            sm_body = string.Empty;
            sm_from = string.Empty;
        }
    }
}
