using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.Admin.Controllers
{
    public class RoleController : Controller
    {
        // GET: Admin/Role
        public ActionResult Index()
        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            return View();
        }
    }
}