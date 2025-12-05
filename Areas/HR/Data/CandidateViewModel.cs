using System;

namespace QLNSVATC.Models
{
    public class CandidateViewModel
    {
        public int ID { get; set; }
        public string TenUngVien { get; set; }
        public string Email { get; set; }

        public string FileThongTin { get; set; }
        public string FileBangCap { get; set; }
        public string FileKhac { get; set; }

        public string FileThongTinUrl { get; set; }
        public string FileBangCapUrl { get; set; }
        public string FileKhacUrl { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
