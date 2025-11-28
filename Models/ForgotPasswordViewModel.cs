using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Models
{
    public class ForgotPasswordViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }

        public string OtpCode { get; set; }

        public string NewPass { get; set; }
        public string ConfirmPass { get; set; }

        public bool ShowOtp { get; set; }
        public bool ShowReset { get; set; }
        public bool Success { get; set; }
    }

}