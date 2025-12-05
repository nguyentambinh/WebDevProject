using System;
using System.Collections.Generic;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Data
{
    public class ProjectCreateViewModel
    {
        public string TypeCode { get; set; }       // LOAI trong HOPDONG
        public string ProjectName { get; set; }    // TENHD 
        public string CustomerId { get; set; }     // MAKH

        public string AreaCode { get; set; }       // MAKV
        public string LoaiHinhCode { get; set; }   // MALH
        public DateTime? StartDate { get; set; }   // NGAYBD
        public DateTime? EndDate { get; set; }     // NGAYKT / NGAYKT_DUTINH

        public decimal? Deposit { get; set; }          // TIENCOC
        public decimal? ExpectedTotal { get; set; }    // TIENNGHIEMTHU_DUTINH
        public decimal? Coefficient { get; set; }      // HSTHAYDOI
        public decimal? FinalTotal { get; set; }       // TIENNGHIEMTHU_TONG
        public string Note { get; set; }             

        public List<DTTHEOKV> Areas { get; set; }
        public List<DTTHEOLHCT> LoaiHinhs { get; set; }
        public List<KHACHHANG> Customers { get; set; }

        public ProjectCreateViewModel()
        {
            Areas = new List<DTTHEOKV>();
            LoaiHinhs = new List<DTTHEOLHCT>();
            Customers = new List<KHACHHANG>();
        }
    }
}
