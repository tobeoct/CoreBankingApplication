﻿using WebApplication1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.ViewModels;
using System.Net.Mail;
using System.Web.UI;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using RestSharp;
using ChangePasswordViewModel = WebApplication1.Models.ChangePasswordViewModel;

namespace WebApplication1.Controllers
{
//   [Authorize(Roles = RoleName.ADMIN_ROLE)]
//   [AllowAnonymous]
    public class UserAccountsController : Controller
    {
        private ApplicationDbContext _context;
        private static Random random;
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private string userId = "";
        private string errorMsg = "";
        public UserAccountsController()
        {
            _context = new ApplicationDbContext();
            random = new Random();
            userId = System.Web.HttpContext.Current.User.Identity.GetUserId();

            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(userId);

            if (user != null)
            {
                RoleName.USER_NAME = user.FullName;
                RoleName.EMAIL = user.Email;
            }


        }



        public UserAccountsController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }


        // GET: UserAccounts
        //[Authorize(Roles = RoleName.ADMIN_ROLE)]
        public ActionResult Index()
        {
            ViewBag.Message = RoleName.USER_NAME;
            var branches = new List<Branch>(_context.Branches.ToList());


            var roles = _context.Roles.Select(r => r.Name);
            var count = _context.Users.Count();
            if (User.IsInRole(RoleName.ADMIN_ROLE))
            {
                return View(new RegisterViewModel { Branch = branches, RolesList = new SelectList(roles), count = count });
            }

            return RedirectToAction("Index", "Branches");

        }
        [Authorize(Roles = RoleName.ADMIN_ROLE)]
        // GET: UserAccounts/EditProfile/1
        public ActionResult EditProfile(string id)
        {
            ViewBag.Message = RoleName.USER_NAME;
            var users = _context.Users.SingleOrDefault(c => c.Id == id);
            var branch = _context.Branches.ToList();
            var viewModel = new EditProfileViewModel
            {
                ApplicationUser = users,
                Branches = branch
            };

            return View(viewModel);
        }

        //        [HttpPost]
        //        public ActionResult ChangePassword(EditProfileViewModel editProfileViewModel)
        //        {
        //            var resetModel = new ResetPasswordViewModel
        //            {
        //                Code = editProfileViewModel.ApplicationUser.ActivationCode.ToString(),
        //                Email = editProfileViewModel.ApplicationUser.Email,
        //                Password = editProfileViewModel.Password
        //            };
        //            return RedirectToAction("ResetPassword", "Account", resetModel);
        //        }
        [HttpGet]
        [Authorize]
        public ActionResult ChangePassword()
        {
            //            var resetModel = new ResetPasswordViewModel
            //            {
            //                Code = editProfileViewModel.ApplicationUser.ActivationCode.ToString(),
            //                Email = editProfileViewModel.ApplicationUser.Email,
            //                Password = editProfileViewModel.Password
            //            };
            ViewBag.Message = "";
            return View("ChangePassword", new ChangePasswordViewModel());
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var email = RoleName.EMAIL;
            ApplicationUser user = await UserManager.FindByEmailAsync(email);
            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var password = getAutoGeneratedPassword();
            //            if (VerificationResetEmail(email, user.Id, resetToken, password, "RESET") == true)
            //            {
            IdentityResult passwordChangeResult = await UserManager.ResetPasswordAsync(user.Id, resetToken, password);
            ViewBag.Message = "Password has been changed";

            //}



            return View("ChangePassword", new ChangePasswordViewModel());



        }
        [HttpPost]
        [Authorize(Roles = RoleName.ADMIN_ROLE)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(EditProfileViewModel model)
        {
            var users = _context.Users.SingleOrDefault(c => c.Id == model.ApplicationUser.Id);
            var branch = _context.Branches.ToList();
            var viewModel = new EditProfileViewModel
            {
                ApplicationUser = users,
                Branches = branch
            };
            var email = model.ApplicationUser.Email;
            ApplicationUser user = await UserManager.FindByEmailAsync(email);
            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var password = getAutoGeneratedPassword();
            if (VerificationResetEmail(email, user.Id, resetToken, password, "RESET") == true)
            {
                IdentityResult passwordChangeResult = await UserManager.ResetPasswordAsync(user.Id, resetToken, password);
                ViewBag.Message = "Reset Email has been sent";
                _context.SaveChanges();
                return View("EditProfile", viewModel);
            }

            ViewBag.Message = errorMsg;

            return View("EditProfile", viewModel);



        }

        // POST: UserAccounts/Save
        [HttpPost]
        [Authorize(Roles = RoleName.ADMIN_ROLE)]
        public ActionResult Save(EditProfileViewModel editProfileViewModel)
        {
            string id = editProfileViewModel.ApplicationUser.Id;
            var user = _context.Users.SingleOrDefault(c => c.Id.Equals(id));
            var branch = _context.Branches.ToList();
            ViewBag.Message = RoleName.USER_NAME;
            if (!ModelState.IsValid)
            {


                var viewModel = new EditProfileViewModel
                {
                    ApplicationUser = user,
                    Branches = branch
                };

                return View("EditProfile", editProfileViewModel);
            }
            var singleUser = _context.Users.Single(c => c.Id.Equals(id));
            singleUser.BranchId = editProfileViewModel.ApplicationUser.BranchId;
            singleUser.FullName = editProfileViewModel.ApplicationUser.FullName;
            singleUser.Email = editProfileViewModel.ApplicationUser.Email;
            singleUser.PhoneNumber = editProfileViewModel.ApplicationUser.PhoneNumber;
            singleUser.Id = editProfileViewModel.ApplicationUser.Id;
            singleUser.UserName = editProfileViewModel.ApplicationUser.UserName;
            _context.SaveChanges();
            editProfileViewModel.ApplicationUser.Branch =
                _context.Branches.SingleOrDefault(b => b.Id == editProfileViewModel.ApplicationUser.BranchId);

            editProfileViewModel.Branches = branch;

            ViewBag.Message = RoleName.USER_NAME;

            return View("EditProfile", editProfileViewModel);
        }
        [HttpPost]
        [Authorize(Roles = RoleName.ADMIN_ROLE)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            bool statusRegistration = false;
            Guid activationCode = Guid.NewGuid();
            string passWord = getAutoGeneratedPassword();
            string messageRegistration = string.Empty;
            var branches = new List<Branch>(_context.Branches.ToList());
            var roles = _context.Roles.Select(r => r.Name);
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    BranchId = model.BranchId,
                    ActivationCode = activationCode

                };
                var result = await UserManager.CreateAsync(user, passWord);
                if (result.Succeeded)
                {
                     await UserManager.AddToRoleAsync(user.Id, model.RoleName);
//                    var roleStore = new RoleStore<IdentityRole>(new ApplicationDbContext());
//                    var roleManager = new RoleManager<IdentityRole>(roleStore);
//                    await roleManager.CreateAsync(new IdentityRole("Teller"));
//                    await UserManager.AddToRoleAsync(user.Id, "Teller");
                    //await SignInManager.SignInAsync(user, isPersistent: true, rememberBrowser: false);

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link

                    //                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    //                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //                    await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                    ViewBag.Message = messageRegistration + passWord;
                    //Verification Email  
                    VerificationEmail(model.Email, activationCode.ToString(), passWord);

                    messageRegistration = "Your account has been created successfully. Check your mail to Activate Your Account";
                    statusRegistration = true;

                    model.BranchId = 0;
                    model.Email = "";
                    model.Username = "";
                    model.FullName = null;
                    model.PhoneNumber = "";

                    //                    return View(new RegisterViewModel { Branch = branches });
                    //                    RedirectToAction("Index", "UserAccounts", new RegisterViewModel { Branch = branches });
                    return View("Index", new RegisterViewModel { Branch = branches, RolesList = new SelectList(roles) });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            //            RedirectToAction("Index", "UserAccounts", new RegisterViewModel { Branch = branches });
            return View("Index", new RegisterViewModel { Branch = branches, RolesList = new SelectList(roles) });

        }
        [NonAction]
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        // POST: GeneralLedgers/CreateAccount
        //        [HttpPost]
        //        public ActionResult CreateAccount(UserAccount userAccount)
        //        {
        //            userAccount.Password = getCode();
        //            SendPasswordByEmailToUser(userAccount.Email, userAccount.Username, userAccount.Password);
        //            _context.UserAccounts.Add(userAccount);
        //            
        //            _context.SaveChanges();
        //            
        //            return RedirectToAction("Index", "UserAccounts");
        //        }
        [NonAction]
        private void SendPasswordByEmailToUser(string email, string username, string password)
        {
            Console.WriteLine("Starting to send email");
            string subject = "Fintech Login Credentials";
            string body = "<div><h1>Login Details</h1><p><b>Username : </b>" + username + "</p><p><b>Password : </b>" + password + "</p></div>";
            string FromMail = "tobe.onyema@gmail.com";
            string emailTo = email;

            MailMessage mm = new MailMessage(FromMail, emailTo)
            {
                Subject = subject,
                Body = body,
                //            if (fuAttachment.HasFile)//file upload select or not
                //            {
                //                string FileName = Path.GetFileName(fuAttachment.PostedFile.FileName);
                //                mm.Attachments.Add(new Attachment(fuAttachment.PostedFile.InputStream, FileName));
                //            }
                IsBodyHtml = true
            };
            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                EnableSsl = true
            };
            NetworkCredential NetworkCred = new NetworkCredential(FromMail, "0801tobe");
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = NetworkCred;
            smtp.Port = 587;
            smtp.Send(mm);
            Response.Write("Send Mail");



        }
        [NonAction]
        public string getCode()
        {
            var GLRanCode = RandomString(8);

            return GLRanCode;

        }


        [NonAction]
        public string getAutoGeneratedPassword()
        {
            var GLRanCode = RandomString(8);

            return GLRanCode;

        }
        [NonAction]
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+}{|:>?<,./";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ActivationAccount(string id)
        {
            bool statusAccount = false;
            var user = new ApplicationUser();
            using (_context)
            {
                ApplicationUser userAccount = _context.Users.FirstOrDefault(x => x.ActivationCode.ToString() == id);

                if (userAccount != null)
                {

                    userAccount.IsActive = true;
                    statusAccount = true;
                    Console.WriteLine(userAccount.IsActive);
                }
                else
                {
                    ViewBag.Message = "Something Wrong !!";
                }

            }
            ViewBag.Status = statusAccount;
            return View("ActivationAccount");
        }

        [NonAction]
        public bool VerificationResetEmail(string email, string activationCode, string id, string password, string type)
        {

            var url = $"/Account/FinalResetPassword/{id}/{activationCode}/{password}";
            if (Request.Url != null)
            {
                var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, url);
                var apiRequest = new RestRequest($"/Account/FinalResetPassword/{HttpUtility.UrlPathEncode(string.Join("+", id))}/{HttpUtility.UrlPathEncode(string.Join("+", activationCode))}/{HttpUtility.UrlPathEncode(string.Join("+", password))}");
                var fromEmail = new MailAddress("tobe.onyema@gmail.com", "Activation Account - FINTECH");
                var toEmail = new MailAddress(email);

                var fromEmailPassword = "0801tobe";
                string subject = "Activation Account ";
                string body = "";
                if (type.Equals("RESET"))
                {
                    body = "<h1>PASSWORD RESET</h1>" +
                                            "<p><b>Username : </b>" + email + "</p>" +
                                            "<p><b>Password : </b>" + password + "</p>";

                    //body = "<br/> Please click on the following link in order to activate your account" + "<a href='" + apiRequest + "' style='font-family:sans-serif;'> Activate your Account </a>" + credentialsBody;
                }
                else
                {

                    body = "<br/> Please click on the following link in order to reset your password" + "<a href='" +
                           link + "' style='font-family:sans-serif;'>Reset Password<a/>";
                }

                try
                {
                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
                    };

                    using (var message = new MailMessage(fromEmail, toEmail)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true

                    })

                        smtp.Send(message);
                }
                catch (SmtpException ex)
                {


                    errorMsg = "Mail cannot be sent because of the server problem:";
                    //  errorMsg += ex.Message;
                    if (errorMsg != "")
                    {
                        return false;
                    }

                    Response.Write(errorMsg);
                    //throw new Exception(msg);

                }

            }

            return true;
        }

        [NonAction]
        public void VerificationEmail(string email, string activationCode, string password)
        {
            var url = string.Format("/Account/ActivationAccount/{0}", activationCode);
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, url);

            var fromEmail = new MailAddress("tobe.onyema@gmail.com", "Activation Account - FINTECH");
            var toEmail = new MailAddress(email);

            var fromEmailPassword = "0801tobe";
            string subject = "Activation Account ";
            string credentialsBody = "<h1>Login Credentials</h1>" +
                          "<p><b>Username : </b>" + email + "</p>" +
                          "<p><b>Password : </b>" + password + "</p>";
            string body = "<br/> Please click on the following link in order to activate your account" + "<a href='" + link + "' style='font-family:sans-serif;'> Activate your Account </a>" + credentialsBody;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true

            })

                smtp.Send(message);

        }

    }

}