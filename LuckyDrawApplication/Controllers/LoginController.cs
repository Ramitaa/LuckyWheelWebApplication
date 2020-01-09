using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace LuckyDrawApplication.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Index(Models.Event luckydrawevent)
        {
            Debug.WriteLine("Event code" + luckydrawevent.EventCode + "Event Password: " + luckydrawevent.EventPassword);

            if (ModelState.IsValid)
            {
                Tuple<bool, int, string> result = DecryptPassword(luckydrawevent.EventCode, luckydrawevent.EventPassword);
                luckydrawevent.EventID = result.Item2;
                luckydrawevent.EventLocation = result.Item3;

                if (result.Item1)
                {
                    Session["event"] = luckydrawevent;
                    return Json(new
                    {
                        success = true,
                        urllink = Url.Action("CreateUserAndDraw", "Home"),
                        message = "Login successful!"
                    }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Authentication failed!"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Authentication failed!"
                }, JsonRequestBehavior.AllowGet);
            }
            
        }

        public ActionResult LogOut()
        {
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();

            return RedirectToAction("Index", "Login");
        }


        [NonAction]
        public static Tuple<bool, int, string> DecryptPassword(string code, string password)
        {
            bool isPasswordMatch = false;
            int eventID = 0;
            string eventLocation = "";

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM event WHERE EventCode = " + code);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                var hash = createPasswordHash(rd["EventSalt"].ToString(), password);

                                if (hash.Equals(rd["EventPassword"].ToString()))
                                {
                                    eventID = Convert.ToInt32(rd["EventID"]);
                                    eventLocation = rd["EventLocation"].ToString();
                                    isPasswordMatch = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return new Tuple<bool, int, string>(isPasswordMatch, eventID, eventLocation);
        }

        [NonAction]
        public static string createPasswordHash(string salt_c, string password)
        {
            int PASSWORD_BCRYPT_COST = 13;
            string PASSWORD_SALT = salt_c;
            string salt = "$2a$" + PASSWORD_BCRYPT_COST + "$" + PASSWORD_SALT;
            var hash = BCrypt.Net.BCrypt.HashPassword(password, salt);

            Debug.WriteLine("Salt_c: " + salt_c, "Hash: " + hash);
            return hash;
        }

        [NonAction]
        public static string getSalt()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 64).Select(s => s[random.Next(s.Length)]).ToArray());
        }


        //----------------------------------------------------------------------------------------------------------------------------------------------
        //                                                              ADMIN CODE
        //----------------------------------------------------------------------------------------------------------------------------------------------

        // GET: Login
        [HttpGet]
        public ActionResult AdminIndex()
        {
            ViewBag.Events = GetEventList();
            return View();
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(Models.ForgotPassword forgotPassword)
        {
            if (ModelState.IsValid)
            {
                Tuple<bool, String> updateToken = SetForgotPasswordToken(forgotPassword.EmailAddress);
                if (updateToken.Item1)
                {
                    var lnkHref = Url.Action("ResetPassword", "Login", new { email = forgotPassword.EmailAddress, token = updateToken.Item2 }, "https");
                    String body = "Hi there!\nYour reset password link is " + lnkHref + ".\nClick on this link to reset your LuckyWheel admin account's password.\n\nBest regards,\nLuckyWheel Team";
                    SendEmail("LuckyWheel | Reset Password Link (Admin Account)", body, forgotPassword.EmailAddress);
                    return RedirectToAction("AdminIndex", "Login");
                }
                else
                {
                    ViewBag.ErrorMessage = "No such email address is registered under LuckyWheel." + updateToken.Item2;
                    return View();
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Please make sure all fields are valid.";
                return View();
            }
        }

        [HttpGet]
        public ActionResult ResetPassword(String email, String token)
        {
            Models.ResetPassword rp = new Models.ResetPassword();
            rp.EmailAddress = email;
            rp.Token = token;

            return View(rp);
        }

        [HttpPost]
        public ActionResult ResetPassword(Models.ResetPassword resetPassword)
        {
            if (ModelState.IsValid)
            {
                if (ResetPasswordInDB(resetPassword))
                {
                    return RedirectToAction("AdminIndex", "Login");
                }
                else
                {
                    ViewBag.ErrorMessage = "The token is invalid. Please reset your password again!";
                    return View();
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Please ensure all fields are valid.";
                return View();
            }
        }

        // POST: LoginAdmin
        [HttpPost]
        public ActionResult AdminIndex(Models.Admin admin)
        {
            Debug.WriteLine("Password: '" + admin.Password);

            if (ModelState.IsValid)
            {
                Tuple<bool, int, string> result = DecryptPasswordForAdmin(admin.Email, admin.Password);
                admin.ID = result.Item2;
                admin.Name = result.Item3;

                if (result.Item1)
                {
                    Models.Event luckydrawevent = GetEventDetails(admin.EventID);

                    Session["admin"] = admin;
                    Session["event"] = luckydrawevent;

                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    ViewBag.Events = GetEventList();
                    ViewBag.ErrorMessage = "Authentication failed!";
                    return View();
                }

            }
            else
            {
                ViewBag.Events = GetEventList();
                ViewBag.ErrorMessage = "Authentication failed! Please make sure all fields are valid.";
                return View();
            }

        }

        [NonAction]
        public static Tuple<bool, int, string> DecryptPasswordForAdmin(string emailAddress, string password)
        {
            bool isPasswordMatch = false;
            int UserID = 0;
            string name = "";

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM adminlogin WHERE emailAddress = '" + emailAddress + "'");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                var hash = createPasswordHash(rd["salt"].ToString(), password);

                                if (hash.Equals(rd["passwordHash"].ToString()))
                                {
                                    UserID = Convert.ToInt32(rd["userID"]);
                                    name = rd["adminname"].ToString();
                                    isPasswordMatch = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return new Tuple<bool, int, string>(isPasswordMatch, UserID, name);
        }

        [NonAction]
        public static Models.Event GetEventDetails(int eventID)
        {
            Models.Event luckydrawevent = new Models.Event();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM event WHERE EventID = " + eventID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                luckydrawevent.EventID = Convert.ToInt32(rd["EventID"]);
                                luckydrawevent.EventCode = (rd["EventCode"]).ToString();
                                luckydrawevent.EventLocation = rd["EventLocation"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return luckydrawevent;
        }

        [NonAction]
        public static void SendEmail(string Subject, string Body, string To)
        {
            try
            {
                using(MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("ramitaa.loganathan98@gmail.com");
                    mail.To.Add(To);
                    mail.Subject = Subject;
                    mail.Body = Body;
                    mail.IsBodyHtml = true;

                    using(SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("ramitaa.loganathan98@gmail.com", "RDJ123Forever@");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }

                }
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        [NonAction]
        public static Tuple<bool, String> SetForgotPasswordToken(String emailAddress)
        {
            bool userExists = false, emailExists = false;
            String token = getSalt();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT COUNT(emailAddress) AS count FROM adminlogin WHERE emailAddress = '" + emailAddress + "'");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            { 
                                if (Convert.ToInt32(rd["count"].ToString()) == 0)
                                    userExists = false;
                                else
                                    userExists = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (userExists)
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                    builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                    builder.UserID = "sqladmin";
                    builder.Password = "luckywheel123@";
                    builder.InitialCatalog = "luckywheeldb";

                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("SELECT COUNT(emailAddress) AS count FROM adminforgotpassword WHERE emailAddress = '" + emailAddress + "'");
                        String sql = sb.ToString();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            connection.Open();
                            using (SqlDataReader rd = command.ExecuteReader())
                            {
                                while (rd.Read())
                                {
                                    if (Convert.ToInt32(rd["count"].ToString()) == 0)
                                        emailExists = false;
                                    else
                                        emailExists = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if (emailExists)
                {
                    token = getSalt();

                    try
                    {
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                        builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                        builder.UserID = "sqladmin";
                        builder.Password = "luckywheel123@";
                        builder.InitialCatalog = "luckywheeldb";

                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("UPDATE adminforgotpassword SET token = '" + token + "' WHERE emailAddress = '" + emailAddress + "'");
                            String sql = sb.ToString();

                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                connection.Open();
                                SqlDataReader rd = command.ExecuteReader();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                else
                {
                    token = getSalt();

                    try
                    {
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                        builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                        builder.UserID = "sqladmin";
                        builder.Password = "luckywheel123@";
                        builder.InitialCatalog = "luckywheeldb";

                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("INSERT INTO adminforgotpassword(emailAddress, token) VALUES('"+ emailAddress + "', '" + token + "')");
                            String sql = sb.ToString();

                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                connection.Open();
                                SqlDataReader rd = command.ExecuteReader();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }

                return new Tuple<bool, String>(true, token);
            }
            else
            {
                return new Tuple<bool, String>(false, "");
            }
        }

        [NonAction]
        public static bool ResetPasswordInDB(Models.ResetPassword resetPassword)
        {
            bool tokenMatches = false;

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT COUNT(emailAddress) AS count FROM adminforgotpassword WHERE token = '" + resetPassword.Token + "' AND emailAddress = '" + resetPassword.EmailAddress + "'");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                if (Convert.ToInt32(rd["count"].ToString()) == 0)
                                    tokenMatches = false;
                                else
                                    tokenMatches = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (tokenMatches)
            {
                string salt = getSalt();
                string passwordHash = createPasswordHash(salt, resetPassword.NewPassword);

                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                    builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                    builder.UserID = "sqladmin";
                    builder.Password = "luckywheel123@";
                    builder.InitialCatalog = "luckywheeldb";

                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("UPDATE adminlogin SET passwordHash = '" + passwordHash + "', salt = '" + salt + "' WHERE emailAddress = '" + resetPassword.EmailAddress + "'");
                        String sql = sb.ToString();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            connection.Open();
                            SqlDataReader rd = command.ExecuteReader();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                return true;

            }
            else
                return false;
        }

        [NonAction]
        public static List<SelectListItem> GetEventList()
        {
            List<SelectListItem> Events = new List<SelectListItem>();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "luckydrawapplication20200108092548dbserver.database.windows.net";
                builder.UserID = "sqladmin";
                builder.Password = "luckywheel123@";
                builder.InitialCatalog = "luckywheeldb";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT * FROM event");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                Events.Add(new SelectListItem() { Text = Convert.ToInt32(rd["EventID"]).ToString() + "- " + rd["EventLocation"].ToString(), Value = Convert.ToInt32(rd["EventID"]).ToString() });
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return Events;
        }
    }
}

