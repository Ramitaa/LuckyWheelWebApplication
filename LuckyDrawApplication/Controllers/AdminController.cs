using LuckyDrawApplication.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LuckyDrawApplication.Controllers
{
    public class AdminController : Controller
    {
        private string response_message = "";

        public ActionResult Index()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            ViewBag.Name = a_user.Name;
            ViewBag.PurchasersList = GetPurchasersCount(luckydrawevent.EventID);
            ViewBag.WinnersList = GetWinnersCount(luckydrawevent.EventID);

            return View();
        }

        [HttpGet]
        public ActionResult CreateUserAndDraw()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            var list = new List<SelectListItem>();
            for (var i = 1; i < 41; i++)
                list.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });

            ViewBag.FloorUnitList = list;
            ViewBag.ProjectList = GetProjectList(luckydrawevent.EventID);
            ViewBag.SalesLocation = luckydrawevent.EventLocation;
            ViewBag.Name = a_user.Name;

            DateTime dateTime = DateTime.UtcNow.Date;

            ViewBag.Date = dateTime.ToString("dd | MM | yyyy").ToString();
            ViewBag.Time = DateTime.Now.ToShortTimeString().ToString();

            return View();
        }

        [HttpPost]
        public ActionResult CreateUserAndDraw(Models.User user)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

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
                    Tuple<int, int> results = CreateNewUser(user, luckydrawevent.EventID);

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
        public ActionResult StaffLuckyDraw()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            if (luckydrawevent == null)
                return RedirectToAction("Index", "Login");

            ViewBag.Name = a_user.Name;

            return View();
        }

        [HttpPost]
        public ActionResult PostStaffLuckyDraw()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            if (luckydrawevent == null)
                return RedirectToAction("Index", "Login");

            ViewBag.Name = a_user.Name;

            Tuple<string, int> results = StaffLuckyDraw(luckydrawevent.EventID);

            if (results.Item1 == null || results.Item1 == "")
            {
                return Json(new
                {
                    success = false,
                    message = "No sales agent to be picked as winner!"
                }, JsonRequestBehavior.AllowGet); ;
            }
            else
            {
                return Json(new
                {
                    success = true,
                    message = results.Item1,
                    urllink = Url.Action("StaffLuckyDrawAnimation", "Admin", new { name = results.Item1.ToUpper(), prize = results.Item2 }, "https"),
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult StaffLuckyDrawAnimation(string name, int prize)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            ViewBag.Name = a_user.Name;
            ViewBag.WinnerName = name;
            ViewBag.WinnerPrize = prize;

            return View();
        }

        [HttpGet]
        public ActionResult LuckyDrawAnimation(int id)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");


            Models.User user = GetUser(id);

            ViewBag.Name = a_user.Name;
            ViewBag.WinnerName = user.Name;
            ViewBag.WinnerPrize = user.PrizeWon;

            return View();
        }


        public ActionResult Users()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            List<User> userList = GetUserList(luckydrawevent.EventID);

            ViewBag.Name = a_user.Name;

            return View(userList);
        }

        public ActionResult Winners()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            List<User> winnerList = GetWinnerList(luckydrawevent.EventID);

            ViewBag.Name = a_user.Name;

            return View(winnerList);
        }

        public ActionResult ViewUser(int id)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            Models.User user = GetUser(id);

            ViewBag.Name = a_user.Name;

            return View(user);
        }

        [HttpGet]
        public ActionResult ModifyUser(int id)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            Models.User user = GetUser(id);
            string[] tokens = user.Unit.Split('-');
            ViewBag.Block = tokens[0];
            ViewBag.Level = tokens[1];
            ViewBag.Unit = tokens[2];

            var list = new List<SelectListItem>();
            for (var i = 1; i < 41; i++)
                list.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });

            ViewBag.FloorUnitList = list;
            ViewBag.ProjectList = GetProjectList(luckydrawevent.EventID);
            ViewBag.SalesLocation = luckydrawevent.EventLocation;
            ViewBag.Name = a_user.Name;

            return View(user);
        }

        [HttpPost]
        public ActionResult ModifyUser(Models.User user)
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            if (user != null)
            {
                if (DuplicateUserExistsForModification(user))
                {
                    return Json(new
                    {
                        success = false,
                        message = "This unit has already been purchased by another buyer!"
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    ModifyExistingUser(user, luckydrawevent.EventID);
                    return Json(new
                    {
                        success = true,
                        url = Url.Action("Users", "Admin"),
                        message = user.Name.ToUpper() + " has been successfully modified!"
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
                    message = user.Name.ToUpper() + " cannot be modified! Error: " + response_message
                }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult ExportToExcelPurchasers()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            var gv = new GridView();
            gv.DataSource = ToDataTable<User>(GetUserList(luckydrawevent.EventID));
            gv.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=DemoExcel.xls");
            Response.ContentType = "application/ms-excel";

            Response.Charset = "";
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);

            gv.RenderControl(objHtmlTextWriter);

            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();

            return View("Index", "Admin");
        }

        public ActionResult ExportToExcelWinners()
        {
            Models.Admin a_user = (Models.Admin)Session["admin"];
            Models.Event luckydrawevent = (Models.Event)Session["event"];

            if (a_user == null || luckydrawevent == null)
                return RedirectToAction("AdminIndex", "Login");

            var gv = new GridView();
            gv.DataSource = ToDataTable<User>(GetWinnerList(luckydrawevent.EventID));
            gv.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=DemoExcel.xls");
            Response.ContentType = "application/ms-excel";

            Response.Charset = "";
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);

            gv.RenderControl(objHtmlTextWriter);

            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();

            return View("Index", "Admin");
        }


        //Generic method to convert List to DataTable
        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Defining type of data column gives proper data table 
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
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
                    sb.Append("INSERT INTO users(Name, ICNumber, EmailAddress, ContactNumber, EventID, ProjectID, Unit, SalesConsultant, PrizeWon, StaffWon) VALUES ('" + user.Name.ToUpper() + "', '" + user.ICNumber + "', '" + user.EmailAddress.ToLower() + "', '" + user.ContactNumber + "', " + eventCode + ", " + user.ProjectID + ", '" + user.Unit.ToUpper() + "', '" + user.SalesConsultant.ToUpper() + "', 0, 0); SELECT SCOPE_IDENTITY() AS id;");
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

        // Get users list
        [NonAction]
        public static List<Models.User> GetUserList(int eventID)
        {
            List<Models.User> UserList = new List<Models.User>();

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
                    sb.Append("SELECT users.*, project.ProjectName, project.PrizeCategory, event.EventLocation FROM users INNER JOIN project on project.ProjectID = users.ProjectID INNER JOIN event ON event.EventID = users.EventID WHERE users.EventID = " + eventID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                Models.User user = new Models.User();
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

                                if (Convert.ToInt32(rd["PrizeWon"]) > 0)
                                {
                                    string[] prizes = rd["PrizeCategory"].ToString().Split(',');
                                    user.PrizeWon = Convert.ToInt32(prizes[Convert.ToInt32(rd["PrizeWon"]) - 1]);
                                }
                                else
                                {
                                    user.PrizeWon = 0;
                                }

                                user.SalesConsultant = rd["SalesConsultant"].ToString();
                                user.DateTime = rd["DateTime"].ToString();
                                UserList.Add(user);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return UserList;
        }

        // Get users list
        [NonAction]
        public static List<Models.User> GetWinnerList(int eventID)
        {
            List<Models.User> UserList = new List<Models.User>();

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
                    sb.Append("SELECT users.*, project.ProjectName, project.PrizeCategory, event.EventLocation FROM users INNER JOIN project on project.ProjectID = users.ProjectID INNER JOIN event ON event.EventID = users.EventID WHERE PrizeWon > 0 AND users.EventID = " + eventID);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                Models.User user = new Models.User();
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
                                UserList.Add(user);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return UserList;
        }

        // Get user
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

        // Modify existing user;
        [NonAction]
        public void ModifyExistingUser(Models.User user, int eventCode)
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
                    sb.Append("UPDATE users SET Name = '" + user.Name.ToUpper() + "' , ICNumber = '" + user.ICNumber + "' , EmailAddress = '" + user.EmailAddress.ToLower() + "' , ContactNumber = '" + user.ContactNumber + "' , EventID = " + eventCode + ", ProjectID = " + user.ProjectID + ", Unit = '" + user.Unit.ToUpper() + "', SalesConsultant = '" + user.SalesConsultant.ToUpper() + "' WHERE PurchaserID = " + user.PurchaserID);
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

        // Check if project and unit clashes;
        [NonAction]
        public static Boolean DuplicateUserExistsForModification(Models.User user)
        {
            Debug.WriteLine("Checking for duplicate" + user.PurchaserID + " projectID: " + user.ProjectID + "unit: " + user.Unit.ToUpper());
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
                    sb.Append("SELECT COUNT(PurchaserID) AS userExists FROM users WHERE ProjectID = " + user.ProjectID + " AND Unit = '" + user.Unit.ToUpper() + "' AND PurchaserID != " + user.PurchaserID);
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

        [NonAction]
        public static List<Project> GetPurchasersCount(int eventID)
        {
            List<Project> projectList = new List<Project>();

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
                                Models.Project project = new Models.Project();
                                project.ProjectID = Convert.ToInt32(rd["ProjectID"]);
                                project.ProjectName = rd["ProjectName"].ToString();
                                project.EventID = eventID;
                                project.NoOfProjects = Convert.ToInt32(rd["NoOfProject"]);
                                projectList.Add(project);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return projectList;
        }

        [NonAction]
        public static List<Project> GetWinnersCount(int eventID)
        {
            List<Project> projectList = new List<Project>();

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
                    sb.Append("SELECT project.ProjectID, MAX(project.ProjectName) AS ProjectName, COUNT(DISTINCT(users.PrizeWon)) AS PrizesWon FROM project INNER JOIN users ON project.ProjectID = users.ProjectID WHERE users.PrizeWon != 0 AND users.EventID = " + eventID + " GROUP BY project.ProjectID");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader rd = command.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                Models.Project project = new Models.Project();
                                project.ProjectID = Convert.ToInt32(rd["ProjectID"]);
                                project.ProjectName = rd["ProjectName"].ToString();
                                project.EventID = eventID;
                                project.NoOfProjects = Convert.ToInt32(rd["PrizesWon"]);
                                projectList.Add(project);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return projectList;
        }

        [NonAction]
        public Tuple<string, int> StaffLuckyDraw(int eventID)
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
                    sb.Append("SELECT TOP 1 * FROM users WHERE users.EventID = " + eventID + " AND users.StaffWon = 0 ORDER BY RAND()");
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
                                user.Unit = rd["Unit"].ToString();
                                user.SalesConsultant = rd["SalesConsultant"].ToString();
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            int orderNo = 0;
            int prizeAmount = 0;
            int won = 0;

            if (user.SalesConsultant != null || user.SalesConsultant != "")
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
                        sb.Append("SELECT TOP 1 * FROM agent WHERE won = 0 ORDER BY RAND()");
                        String sql = sb.ToString();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            connection.Open();
                            using (SqlDataReader rd = command.ExecuteReader())
                            {
                                while (rd.Read())
                                {
                                    orderNo = Convert.ToInt32(rd["orderNo"]);
                                    prizeAmount = Convert.ToInt32(rd["prizeAmount"]);
                                    won = Convert.ToInt32(rd["won"]);
                                }
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine(e.ToString());
                }

                UpdateDatabaseAfterStaffWon(user, orderNo, prizeAmount);
            }

            return new Tuple<String, int>(user.SalesConsultant, prizeAmount);
        }

        // Modify existing user;
        [NonAction]
        public void UpdateDatabaseAfterStaffWon(Models.User user, int orderNo, int prizeAmount)
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
                    sb.Append("UPDATE users SET StaffWon = " + prizeAmount + " WHERE PurchaserID = " + user.PurchaserID);
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
                    sb.Append("UPDATE agent SET won = 1 WHERE orderNo = " + orderNo);
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

    }
}