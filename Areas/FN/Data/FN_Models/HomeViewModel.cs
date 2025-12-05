using System.Collections.Generic;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{ 
    public class HomeViewModel
    {
        public List<DUAN> DuAn { get; set; }
        public List<HOPDONG> HopDong { get; set; }
        public List<DUANTHEOHOPDONG> DuAnTheoHopDong { get; set; }
        public List<KHACHHANG> KhachHang { get; set; }
        public List<NHANVIEN> NhanVien { get; set; }

        public int TotalNhanVien { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize == 0) return 1;
                return (TotalNhanVien + PageSize - 1) / PageSize;
            }
        }
    }
}
