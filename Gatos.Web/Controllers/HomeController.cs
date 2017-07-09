using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            ViewBag.Message = "About us.";

            return View();
        }

        public ActionResult Contact()
        {
            GatherContactInformation();
            ViewBag.Message = "Contact us.";

            return View();
        }

        private void GatherContactInformation()
        {
            var hqDetails = new ContactDetails();
            var officeDetails = new ContactDetails();
            ProcessDetails(hqDetails, officeDetails);
        }

        private string ProcessDetails(ContactDetails first, ContactDetails second)
        {
            lock (first)
            {
                Task.Run(() => ProcessDetails(second, first));
                Thread.Sleep(200);
                lock (second)
                {
                    return "Merged contacts: " + first.ToString() + second.ToString();
                }
            }
        }
    }

    internal class ContactDetails
    {
    }
}