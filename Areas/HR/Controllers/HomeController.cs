using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class HomeController : Controller
    {
        // GET: HR/Home
        public ActionResult Index()
        {
            if (!CheckAccess.Role("HR"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View();
        }

    }
}