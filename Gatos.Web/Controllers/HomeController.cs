using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Gatos.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            for (int i = 0; i < 100000; ++i)
            {
                doc.DocumentElement.AppendChild(doc.CreateElement("element" + i));
            }
            doc.OuterXml.ToString();

            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}