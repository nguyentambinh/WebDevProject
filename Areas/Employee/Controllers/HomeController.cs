using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.Employee.Controllers
{
    public class HomeController : Controller
    {
        // GET: Employee/Home
        public ActionResult Index()
        {
            if (!CheckAccess.Role("EM"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View();
        }
    }
}