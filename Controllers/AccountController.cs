using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;

namespace QLNSVATC.Controllers
{
    public class AccountController : Controller
    {
        private QLNSVATCEntities db = new QLNSVATCEntities();

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string pass)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pass))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                return View(new USER { USERNAME = username });
            }

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại!");
                return View(new USER { USERNAME = username });
            }

            if (user.PASS != pass)
            {
                ModelState.AddModelError("", "Mật khẩu không đúng!");
                return View(new USER { USERNAME = username });
            }
            string auth = user.AUTH;
            var authInfo = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == auth);

            if (authInfo == null)
            {
                ModelState.AddModelError("", "Không tìm thấy quyền trong hệ thống!");
                return View(new USER { USERNAME = username });
            }

            string codeBus = authInfo.CODEBUS;
            string prefix = auth.Substring(0, 2).ToUpper();
            string code = authInfo.CODE;
            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == code);
            if(nv != null)
            {
                Session["UserId"] = nv.MANV;
                Session["FullName"] = nv.TENNV;
                Session["Role"] = auth;
            }
            
            switch (prefix)
            {
                case "AD":
                    return RedirectToAction("Index", "Home", new { area = "Admin" });

                case "HR":
                    return RedirectToAction("Index", "Home", new { area = "HR", id = codeBus });

                case "FN":
                    return RedirectToAction("Index", "Home", new { area = "FN", id = codeBus });

                case "OF":
                    return RedirectToAction("Index", "Home", new { area = "OF", id = codeBus });

                case "EM":
                    return RedirectToAction("Index", "Home", new { area = "Employee", id = codeBus });

                default:
                    ModelState.AddModelError("", "Phân quyền không hợp lệ!");
                    return View(new USER { USERNAME = username });
            }
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
