using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Web.Mvc;

namespace LuckyDrawApplication.Controllers
{
    public class HomeController : Controller
    {
        private string response_message = "";

        [HttpGet]
        public ActionResult CreateUserAndDraw()
        {
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (luckydrawevent == null)
                return RedirectToAction("Index", "Login");

            var list = new List<SelectListItem>();
            for (var i = 1; i < 41; i++)
                list.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });

            ViewBag.FloorUnitList = list;
            ViewBag.ProjectList = GetProjectList(1);
            ViewBag.SalesLocation = luckydrawevent.EventLocation;

            DateTime dateTime = DateTime.UtcNow.Date;

            ViewBag.Date = dateTime.ToString("dd | MM | yyyy").ToString();
            ViewBag.Time = DateTime.Now.ToShortTimeString().ToString();

            return View();
        }

        [HttpPost]
        public ActionResult CreateUserAndDraw(Models.User user)
        {
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (luckydrawevent == null)
                return RedirectToAction("Index", "Login");

            if (user != null)
            {
                if (DuplicateUserExists(user))
                {
                    return Json(new
                    {
                        success = false,
                        draw = -1,
                        message = "This unit has already been purchased by another buyer!"
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Tuple<int, int> results = CreateNewUser(user, 1);

                    return Json(new
                    {
                        success = true,
                        draw = results,
                        urllink = Url.Action("LuckyDrawAnimation", "Home", new { id = results.Item1 }, "https"),
                        message = user.Name.ToUpper() + " has been registered successfully!"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                response_message = "User is null!";

                return Json(new
                {
                    success = false,
                    draw = -1,
                    message = user.Name.ToUpper() + " cannot be registered! Error: " + response_message
                }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpGet]
        public ActionResult LuckyDrawAnimation(int id)
        {
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (luckydrawevent == null)
                return RedirectToAction("Index", "Login");

            Models.User user = GetUser(id);

            ViewBag.WinnerName = user.Name;
            ViewBag.WinnerPrize = user.PrizeWon;

            return View();
        }

        // Register new user;
        [NonAction]
        public Tuple<int, int> CreateNewUser(Models.User user, int eventCode)
        {
            int last_inserted_id = 0;
            response_message = "";

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
                    sb.Append("INSERT INTO users(Name, ICNumber, EmailAddress, ContactNumber, EventID, ProjectID, Unit, SalesConsultant, PrizeWon, StaffWon) VALUES ('" + user.Name.ToUpper() + "', '" + user.ICNumber + "', '" + user.EmailAddress.ToLower() + "', '" +  user.ContactNumber + "', " + eventCode + ", " + user.ProjectID + ", '" + user.Unit.ToUpper() + "', '" + user.SalesConsultant.ToUpper() + "', 0, 0); SELECT SCOPE_IDENTITY() AS id;");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                last_inserted_id = Convert.ToInt32(rd["id"]);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

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
                    sb.Append("UPDATE project SET NoOfProject= NoOfProject + 1 WHERE ProjectID = " + user.ProjectID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        SqlDataReader rd = command.ExecuteReader();
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return CheckForLuckyDraw(user, last_inserted_id);
        }

        [NonAction]
        public static Tuple<int, int> CheckForLuckyDraw(Models.User user, int last_inserted_id)
        {
            string prizeCategory = "";
            int prizeCode = 0;
            int prizeAmount = 0;

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
                    sb.Append("SELECT project.PrizeCategory AS prizeCategory, luckydraw.Prize AS prize FROM project INNER JOIN luckydraw ON project.ProjectID = luckydraw.ProjectID WHERE luckydraw.ProjectID = " + user.ProjectID + " AND luckydraw.OrderNo = project.NoOfProject");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                prizeCategory = rd["prizeCategory"].ToString();
                                prizeCode = Convert.ToInt32(rd["prize"]);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            Debug.WriteLine("Prize Category: " + prizeCategory + ", PrizeCode: " + prizeCode);

            if (prizeCode != 0)
            {
                string[] prizes = prizeCategory.Split(',');
                prizeAmount = Convert.ToInt32(prizes[prizeCode - 1]);
                UpdateDatabaseWhenLuckyDrawIsWon(last_inserted_id, prizeCode);
                return new Tuple<int, int>(last_inserted_id, prizeAmount);
            }

            else
            {
                return new Tuple<int, int>(last_inserted_id, 0);
            }
        }

        [NonAction]
        public static void UpdateDatabaseWhenLuckyDrawIsWon(int userID, int prizeCode)
        {
            Debug.WriteLine("Updating Database with PrizeCode: " + prizeCode);

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
                    sb.Append("UPDATE users SET PrizeWon = " + prizeCode + " WHERE PurchaserID =  " + userID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        SqlDataReader rd = command.ExecuteReader();
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Check if ic, project and unit clashes;
        [NonAction]
        public static Boolean DuplicateUserExists(Models.User user)
        {
            int count = 0;

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
                    sb.Append("SELECT COUNT(PurchaserID) AS userExists FROM users WHERE ProjectID = " + user.ProjectID + " AND Unit = '" + user.Unit.ToUpper() + "'");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                count = Convert.ToInt32(rd["userExists"].ToString());
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return count > 0;
        }

        [NonAction]
        public static List<SelectListItem> GetProjectList(int eventID)
        {
            List<SelectListItem> Projects = new List<SelectListItem>();

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
                    sb.Append("SELECT * FROM project WHERE EventID = " + eventID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                Projects.Add(new SelectListItem() { Text = rd["ProjectName"].ToString(), Value = Convert.ToInt32(rd["ProjectID"]).ToString() });
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return Projects;
        }

        // Get users list
        [NonAction]
        public static Models.User GetUser(int userID)
        {
            Models.User user = new Models.User();

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
                    sb.Append("SELECT project.ProjectName, project.PrizeCategory, event.EventLocation, users.* FROM users INNER JOIN project on project.ProjectID = users.ProjectID INNER JOIN event ON event.EventID = users.EventID WHERE PurchaserID = " + userID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                user.PurchaserID = Convert.ToInt32(rd["PurchaserID"].ToString());
                                user.Name = rd["Name"].ToString();
                                user.ICNumber = rd["ICNumber"].ToString();
                                user.EmailAddress = rd["EmailAddress"].ToString();
                                user.ContactNumber = rd["ContactNumber"].ToString();
                                user.EventID = Convert.ToInt32(rd["EventID"].ToString());
                                user.ProjectID = Convert.ToInt32(rd["ProjectID"].ToString());
                                user.ProjectName = rd["ProjectName"].ToString();
                                user.SalesLocation = rd["EventLocation"].ToString();
                                user.Unit = rd["Unit"].ToString();
                                user.SalesConsultant = rd["SalesConsultant"].ToString();

                                if (Convert.ToInt32(rd["PrizeWon"]) > 0)
                                {
                                    string[] prizes = rd["PrizeCategory"].ToString().Split(',');
                                    user.PrizeWon = Convert.ToInt32(prizes[Convert.ToInt32(rd["PrizeWon"]) - 1]);
                                }
                                else
                                {
                                    user.PrizeWon = 0;
                                }

                                user.DateTime = rd["DateTime"].ToString();
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return user;
        }

    }
}