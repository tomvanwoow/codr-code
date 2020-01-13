﻿using Codr.Data;
using Codr.Models;
using Codr.Models.Posts;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Linq;

namespace Codr.Controllers {
    public class AppController : Controller {
        private readonly UserProvider session = new UserProvider(DocumentStoreHolder.Store.OpenSession());
        private User? ThisUser {
            get {
                if (Request.Cookies.TryGetValue("Session", out string id)) {
                    Thread.Sleep(1000); // what 
                    return session.GetUserBySessionId(id);
                }
                return null;
            }
        }

        public IActionResult Index() {
            if (ThisUser is { } u) {
                return View(u);
            }
            Response.Cookies.Delete("Session");
            return Redirect("/");
        }

        public IActionResult Friends(int page = 0) {
            if (ThisUser is { } u) {
                int maxPage = ((u.Friends.Count - 1) / 10) + 1;
                if (u.Friends.Count != 0 && page > maxPage)
                    page = maxPage;

                ViewData["Page"] = page;
                return View(u);
            }
            Response.Cookies.Delete("Session");
            return Redirect("/");
        }

        public IActionResult Profile(string id) {
            if (ThisUser is { } u) {
                if (string.IsNullOrEmpty(id)) {
                    return View(u);
                }
                var user = session.GetUser(id);
                return View(user);
            }
            Response.Cookies.Delete("Session");
            return Redirect("/");
        }

        [Route("App/Profile/AddComment")]
        [HttpGet]
        public IActionResult AddComment() {
            return Redirect("/App");
        }

        [Route("App/Profile/AddComment")]
        [HttpPost]
        public IActionResult AddComment(string postId, string content) {
            if (ThisUser is { } u) {
                var post = session.Session.Load<Post?>(postId);
                if (post is { } p) {
                    var comment = new Comment(u.Id, content);
                    session.Session.Store(comment);
                    session.Session.SaveChanges();
                    p.Comments.Add(session.Session.Advanced.GetDocumentId(comment));
                    session.Session.SaveChanges();
                    return Ok();
                }
            }
            return BadRequest();
        }

        [HttpGet]
        public IActionResult GetComments(string id) {
            var post = session.Session.Load<Post?>(id);
            if (post is { } p) {
                return Json(p.Comments.Select(s => session.Session.Load<Comment?>(s)).OrderBy(c => c?.Likes ?? 0));
            }
            var comment = session.Session.Load<Comment?>(id);
            if (comment is { } c) {
                return Json(c.Replies.Select(s => session.Session.Load<Comment?>(s)).OrderBy(c => c?.Likes ?? 0));
            }
            return Json(null);
        }
    }
}
