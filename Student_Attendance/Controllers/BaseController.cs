﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using System.Linq;

namespace Student_Attendance.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        public User CurrentUser { get; set; }

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Retrieve institute info (adjust as needed to your data model)
            var institute = _context.Institutes.FirstOrDefault();
            ViewBag.ShortName = institute?.ShortName ?? "SAS";
            ViewBag.Logo = string.IsNullOrEmpty(institute?.Logo) ? "/images/default-logo.png" : institute.Logo;
            base.OnActionExecuting(context);

            string username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = _context.Users.FirstOrDefault(u => u.UserName == username);
            }
            else
            {
                CurrentUser = null;
            }
        }
    }
}