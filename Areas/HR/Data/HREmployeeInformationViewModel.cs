using System;

namespace QLNSVATC.Models
{
    public class HREmployeeInformationViewModel
    {
        public string MaNV { get; set; }
        public string HoLot { get; set; }
        public string TenNV { get; set; }
        public bool GioiTinh { get; set; }
        public short? NamSinh { get; set; }
        public int? Tuoi { get; set; }

        public string MaPB { get; set; }
        public string TenPhongBan { get; set; }
        public string MaCV { get; set; }
        public string TenChucVu { get; set; }

        public string MaDN { get; set; }
        public string TenDoanhNghiep { get; set; }
        public string DiaChiDoanhNghiep { get; set; }

        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayHDLD { get; set; }

        public string QueQuanText { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string Facebook { get; set; }

        public string LoaiNV { get; set; }
        public double? HeSoLuong { get; set; }
        public decimal? LuongCoBan { get; set; }

        public string NguoiThanTen { get; set; }
        public string NguoiThanQuanHe { get; set; }
        public string NguoiThanSDT { get; set; }
        public string NguoiThanDiaChi { get; set; }

        public double? TongCa3Thang { get; set; }
        public decimal? TongThuong { get; set; }
        public decimal? TongPhat { get; set; }

        public string AvatarPath { get; set; }

        public byte? ChieuCao { get; set; }       
        public byte? CanNang { get; set; }           
        public string TienSuBenh { get; set; }        
        public byte? ThiLucTren10 { get; set; }        
        public DateTime? NgayCapNhatSucKhoe { get; set; } 

        public string LoaiBaoHiem { get; set; }     
        public string SoBaoHiem { get; set; }      
        public DateTime? ThoiHanBaoHiem { get; set; }  
    }
}
