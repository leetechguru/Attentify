using System.ComponentModel.DataAnnotations;

namespace GoogleLogin.Models
{
    public class TbEmail
    {
        [Key]
        public long em_idx { get; set; }
        public string em_id { set; get; }
        public string? em_subject { set; get; }
        public string? em_body { set; get; }
        public string em_from { set; get;}
        public string em_to { set; get;}
        public string? em_replay { set; get;}
        public Nullable<int> em_state { set; get;}
        public string? em_threadId { set; get;}
        public Nullable<int> em_level { set; get; }
        public Nullable<DateTime> em_date { set; get; }
        public Nullable<int> em_read { set; get; }
    }
}
