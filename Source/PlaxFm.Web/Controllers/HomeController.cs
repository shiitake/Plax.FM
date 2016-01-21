using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using PlaxFm.Web.Models;
using PlaxFm.Core;

namespace PlaxFm.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly IList<CommentModel> _comments;
        private static readonly ConfigModel _config;

        static HomeController()
        {
            _comments = new List<CommentModel>
            {
                new CommentModel
                {
                    Author = "Danny Lo Nigro",
                    Text = "Hello ReactJS.NET Cleveland!"
                },
                new CommentModel
                {
                    Author = "DudeFace Mahonee",
                    Text = "Have a comment! Have **several** comments!"
                },
                new CommentModel
                {
                    Author = "Narles Garklu",
                    Text = "I told you, that is *not* my real name."
                }
            };
            _config = 
        }
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddComment(CommentModel comment)
        {
            _comments.Add(comment);
            return Content("Booyah - Commented! ;) ");
        }

        [OutputCache(Location = OutputCacheLocation.None)]
        public ActionResult Comments()
        {
            return Json(_comments, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Config()
        {
            
        }
    }
}