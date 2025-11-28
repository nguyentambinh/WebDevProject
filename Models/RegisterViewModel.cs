using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Models
{
    public class RegisterViewModel
    {
        public string EmployeeCode { get; set; }
        public string Email { get; set; }
        public string USERNAME { get; set; }
        public string PASS { get; set; }
        public string ConfirmPassword { get; set; }
        public string OtpCode { get; set; }
        public bool IsSuccess { get; set; }
    }

}