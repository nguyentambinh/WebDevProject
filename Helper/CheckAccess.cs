using System.Web;

namespace QLNSVATC.Helper
{
    public static class CheckAccess
    {
        // Kiểm tra phân quyền: "AD", "HR", "FN", "EM", "OF",...
        public static bool Role(string requiredPrefix)
        {
            var context = HttpContext.Current;
            if (context == null) return false;

            var session = context.Session;
            if (session == null ||
                session["UserId"] == null ||
                session["Role"] == null)
                return false;

            string role = session["Role"].ToString().ToUpper();
            return role.StartsWith(requiredPrefix.ToUpper());
        }
    }
}
