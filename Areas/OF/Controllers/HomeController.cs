using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.OF.Controllers
{
    public class HomeController : Controller
    {
        // GET: OF/OF
        public ActionResult Index()
        {
            if (!CheckAccess.Role("OF"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View();
        }
    }
}