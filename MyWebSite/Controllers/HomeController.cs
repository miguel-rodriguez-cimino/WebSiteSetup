using MyLibrary;
using MyWebSite.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace MyWebSite.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HomeModel model)
        {
            using (DatabaseContext context = new DatabaseContext())
            {
                ViewBag.Message = context.Messages.First().Text;
            }

            return View(model);
        }

        public ActionResult Error(HttpStatusCode id)
        {
            string errorMessage = string.Empty;
            switch (id)
            {
                case HttpStatusCode.BadRequest:
                    errorMessage = string.Format(CultureInfo.InvariantCulture, "Error {0}: La operación solicitada no es válida.", (int)id);
                    break;
                case HttpStatusCode.Forbidden:
                    errorMessage = string.Format(CultureInfo.InvariantCulture, "Error {0}: La operación que usted ha realizado no esta permitida.", (int)id);
                    break;
                case HttpStatusCode.NotFound:
                    errorMessage = string.Format(CultureInfo.InvariantCulture, "Error {0}: La página solicitada no existe.", (int)id);
                    break;
                default:
                    errorMessage = string.Format(CultureInfo.InvariantCulture, "Error {0}: Error del cliente no especifico.", (int)id);
                    break;
            }

            Response.StatusCode = (int)id;
            ViewData.Model = errorMessage;
            return this.View("ClientRequestError");
        }
    }
}