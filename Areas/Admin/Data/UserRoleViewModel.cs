using System;

namespace QLNSVATC.Areas.Admin.Data
{
    public class UserRoleViewModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PositionName { get; set; }
        public string BusinessCode { get; set; }

        public string RoleGroup { get; set; }
        public string RoleGroupName { get; set; }

        public string AuthCode { get; set; }

        public DateTime? JoinDate { get; set; }
        public bool TwoFAEnabled { get; set; }
    }
}
