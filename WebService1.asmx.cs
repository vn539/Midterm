using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Data.SqlClient;
using System.Configuration;

namespace SoapService
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        [WebMethod]
        //  public string HelloWorld()
        //  {
        //      return "Hello World";
        //  }
        public string getLocation()
        {
            return "39.034411,-94.572049";
        }

        [WebMethod]
        public string GetAddress1()
        {
            return "Address 1 = 15090 W 119th St, Olathe, KS 66062";
        }

        [WebMethod]
        public string GetAddress2()
        {
            return "Address 2 = 6750 W 95th St, Overland Park, KS 66212";
        }

        [WebMethod]
        public List<Review> queryTable()
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbReview"].ConnectionString);
            conn.Open();

            string age = "";
            string name = "";
            string city = "";
            string review = "";
            var reviews = new List<Review>();

            SqlCommand cmd = new SqlCommand("Select * From [Reviews Table]", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var reviewItem = new Review();
                reviewItem.Age = "" + reader["age"];
                reviewItem.Name = "" + reader["name"].ToString();
                reviewItem.City = "" + reader["city"].ToString();
                reviewItem.ReviewText = "" + reader["reviews"].ToString();

                reviews.Add(reviewItem);
            }
            reader.Close();
            conn.Close();
            //return "info:" + name + "," + age + "," + city + "," + review;
            return reviews;
        }

        [WebMethod]
        public string InsertReview(string name, string age, string city, string reviewText)
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbReview"].ConnectionString);
            conn.Open();

            //Declare the sql command
            SqlCommand cmd = new SqlCommand
                ("Insert into [Reviews Table](name,age,city,reviews)values('" + name + "','" + age + "','" + city + "','" + reviewText + "')", conn);

            //Execute the insert query
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            //close the connection
            conn.Close();
            
            return "Review successfully added";
        }

        [WebMethod]
        public string RemoveReview(string reviewName)
        {
            //Declare Connection by passing the connection string from the web config file
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbReview"].ConnectionString);

            //Open the connection
            conn.Open();

            //Declare the sql command
            SqlCommand cmd = new SqlCommand("delete from [Reviews Table] where name= '" + reviewName + "'", conn);

            //Execute the insert query
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            //close the connection
            conn.Close();

            return "Review successfully deleted";
        }


        [WebMethod]
        public string getCityNotes(string City)
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["neighbourhoodNotes"].ConnectionString);
            conn.Open();

            string cityNotes = "";
            
            SqlCommand cmd = new SqlCommand("Select * From [CityNotes] where cityname = @city", conn);
            SqlParameter cityParam = new SqlParameter("city", City);
            cmd.Parameters.Add(cityParam);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                cityNotes += reader["citynotes"];
            }
            reader.Close();
            conn.Close();
            //return "info:" + name + "," + age + "," + city + "," + review;
            return cityNotes;
        }

        [WebMethod]
        public string StoreRoute(string data)
        {
            //use the JavaScriptSerializer to convert the string to a CompositeType instance
            var jscript = new JavaScriptSerializer();
            var newRouteInfo = jscript.Deserialize<RouteInfo>(data);

            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbRoutes"].ConnectionString);
            conn.Open();

            // check for duplicate name
            SqlCommand cmd = new SqlCommand("Select * From [Routes] where routename = @name", conn);
            SqlParameter routenameParam = new SqlParameter("name", newRouteInfo.RouteName);
            cmd.Parameters.Add(routenameParam);
            SqlDataReader reader = cmd.ExecuteReader();
            bool isDup = reader.HasRows;
            reader.Close();
            cmd.Dispose();

            if (isDup)
            {
                return "A route with this name already exists. Please specify a different name.";               
            }


            //Declare the sql command
            var cmdRoute = new SqlCommand
                ("Insert into [Routes](RouteName,RouteMode,Description,IsFree,ImageUrl)values('" + newRouteInfo.RouteName + "','" +
                        newRouteInfo.RouteMode + "','" + newRouteInfo.Description + "','" + "1" + "','" + newRouteInfo.ImageUrl + "')", conn);

            //Execute the insert query
            cmdRoute.ExecuteNonQuery();
            foreach (var waypoint in newRouteInfo.MarkerList)
            {
                cmdRoute = new SqlCommand
                    ("Insert into [RouteWaypoints](RouteName,lat,lng,Title, Description,ImageUrl)values('" + newRouteInfo.RouteName + "','" +
                            waypoint.Lat + "','" + waypoint.Lng + "','" + waypoint.Title + "','" + waypoint.Description + "','" + waypoint.ImageUrl + "')", conn);

                //Execute the insert query
                cmdRoute.ExecuteNonQuery();
            }

            cmdRoute.Dispose();
            //close the connection
            conn.Close();

            return "success";
        }

        [WebMethod]
        public List<RouteInfo> GetRoutes()
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbRoutes"].ConnectionString);
            conn.Open();

            var routes = new List<RouteInfo>();

            SqlCommand cmd = new SqlCommand("Select * From [Routes]", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var routeItem = new RouteInfo();
                routeItem.RouteName = "" + reader["RouteName"];
                routeItem.RouteMode = "" + reader["RouteMode"].ToString();
                routeItem.Description = "" + reader["Description"].ToString();
                routeItem.IsFree = (!String.IsNullOrEmpty(reader["IsFree"].ToString()) && reader["IsFree"].Equals("1"));
                routeItem.ImageUrl = "" + reader["ImageUrl"].ToString();

                routes.Add(routeItem);
            }

            reader.Close();

            foreach (var route in routes)
            {
                cmd = new SqlCommand("Select * From [RouteWaypoints] where routename = @name", conn);
                SqlParameter routenamewayParam = new SqlParameter("name", route.RouteName);
                cmd.Parameters.Add(routenamewayParam);
                SqlDataReader readerWaypoint = cmd.ExecuteReader();

                var routeWPList = new List<Position>();

                while (readerWaypoint.Read())
                {
                    var routeWaypointItem = new Position();
                    routeWaypointItem.Lat = "" + readerWaypoint["Lat"];
                    routeWaypointItem.Lng = "" + readerWaypoint["Lng"].ToString();
                    routeWaypointItem.Description = "" + readerWaypoint["Description"].ToString();
                    routeWaypointItem.Title = "" + readerWaypoint["Title"].ToString();
                    routeWaypointItem.ImageUrl = "" + readerWaypoint["ImageUrl"].ToString();

                    routeWPList.Add(routeWaypointItem);
                }

                route.MarkerList = routeWPList.ToArray();
                readerWaypoint.Close();
            }

            cmd.Dispose();
            conn.Close();
            return routes;
        }

        [WebMethod]
        public List<RouteInfo> SearchRoutes(string routename)
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbRoutes"].ConnectionString);
            conn.Open();

            var routes = new List<RouteInfo>();

            SqlCommand cmd = new SqlCommand("Select * From [Routes] where routename = @name", conn);
            SqlParameter routenameParam = new SqlParameter("name", routename);
            cmd.Parameters.Add(routenameParam);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var routeItem = new RouteInfo();
                routeItem.RouteName = "" + reader["RouteName"];
                routeItem.RouteMode = "" + reader["RouteMode"].ToString();
                routeItem.Description = "" + reader["Description"].ToString();
                routeItem.IsFree = (!String.IsNullOrEmpty(reader["IsFree"].ToString()) && reader["IsFree"].Equals("1"));
                routeItem.ImageUrl = "" + reader["ImageUrl"].ToString();

                routes.Add(routeItem);
            }

            reader.Close();

            foreach (var route in routes)
            {
                cmd = new SqlCommand("Select * From [RouteWaypoints] where routename = @name", conn);
                SqlParameter routenamewayParam = new SqlParameter("name", route.RouteName);
                cmd.Parameters.Add(routenamewayParam);
                SqlDataReader readerWaypoint = cmd.ExecuteReader();

                var routeWPList = new List<Position>();

                while (readerWaypoint.Read())
                {
                    var routeWaypointItem = new Position();
                    routeWaypointItem.Lat = "" + readerWaypoint["Lat"];
                    routeWaypointItem.Lng = "" + readerWaypoint["Lng"].ToString();
                    routeWaypointItem.Description = "" + readerWaypoint["Description"].ToString();
                    routeWaypointItem.Title = "" + readerWaypoint["Title"].ToString();
                    routeWaypointItem.ImageUrl = "" + readerWaypoint["ImageUrl"].ToString();

                    routeWPList.Add(routeWaypointItem);
                }

                route.MarkerList = routeWPList.ToArray();
                readerWaypoint.Close();
            }


            cmd.Dispose();
            conn.Close();
            return routes;
        }

    }

    // Data contract for storing a new route.
    public class RouteInfo
    {
        public string RouteName { get; set; }
        public string RouteMode { get; set; }
        public string Description { get; set; }
        public bool IsFree { get; set; }
        public string ImageUrl { get; set; }
        public Position[] MarkerList { get; set; }
    }

    public class Position
    {
        public string Lat { get; set; }
        public string Lng { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }


    public class Review
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public string City { get; set; }
        public string ReviewText { get; set; }
    }

}
