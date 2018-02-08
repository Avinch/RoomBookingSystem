﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;

namespace Main_RBS
{
    public class DatabaseHelper
    {
        private string connectionString;

        private SqlConnection connection;

        public string getCString()
        {
            connectionString = ConfigurationManager.ConnectionStrings["Main_RBS.Properties.Settings.dbConnectionString"].ConnectionString;
            return connectionString;
        }

        public List<ListViewItem> popBookings(bool all = true)
        {
            SqlConnection connection;

            List<ListViewItem> listItems = new List<ListViewItem>();

            string query;

            if (session.userID < 0 || all)
            {
                //query = "SELECT b.*, u.Username FROM tblBookings b, tblUsers u";
                //query = "SELECT b.* FROM tblBookings b LEFT JOIN tblUsers u ON CONVERT(varchar, b.UserID) = u.Username;";
                query = "SELECT b.* FROM tblBookings b ";
            }
            else
            {
                query = String.Format("SELECT * FROM tblBookings WHERE UserID = {0} ORDER BY TimeBooked DESC", session.userID);
            }

            Debug.WriteLine(String.Format("Sending SQL command: {0}", query));

            try
            {
                using (connection = new SqlConnection(getCString()))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

                    DataTable dt = new DataTable();

                    try
                    {
                        adapter.Fill(dt);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("it's this");
                        Debug.WriteLine(ex.ToString());
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow dr = dt.Rows[i];

                        string convertedDT = Convert.ToDateTime(dr["Date"].ToString()).ToShortDateString();

                        string[] list = new string[] { dr["RoomID"].ToString(), convertedDT, dr["Period"].ToString(), dr["UserID"].ToString(), dr["TimeBooked"].ToString(), dr["Id"].ToString(), dr["Notes"].ToString() };

                        ListViewItem li = new ListViewItem(list);

                        listItems.Add(li);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return listItems;
        }

        public List<booking> populateCalendar(int roomID, int period)
        {
            SqlConnection connection;

            List<booking> listItems = new List<booking>();

            string query;

            query = String.Format("SELECT Date, Id, UserID FROM tblBookings WHERE RoomID = {0} AND Period = {1}", roomID.ToString(), period.ToString());

            Debug.WriteLine(String.Format("Sending SQL command: {0}", query));

            try
            {
                using (connection = new SqlConnection(getCString()))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

                    DataTable dt = new DataTable();

                    try
                    {
                        adapter.Fill(dt);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("it's this");
                        Debug.WriteLine(ex.ToString());
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow dr = dt.Rows[i];

                        /*string convertedDT = Convert.ToDateTime(dr["Date"].ToString()).ToShortDateString();

                        string[] list = new string[] { dr["RoomID"].ToString(), convertedDT, dr["Period"].ToString(), dr["UserID"].ToString(), dr["TimeBooked"].ToString(), dr["Id"].ToString(), dr["Notes"].ToString() };

                        ListViewItem li = new ListViewItem(list);

                        listItems.Add(li);*/

                        booking bk = new Main_RBS.booking();
                        bk.date = Convert.ToDateTime(dr["Date"].ToString());
                        bk.id = Convert.ToInt32(dr["Id"]);
                        bk.UserID = Convert.ToInt32(dr["UserID"]);

                        listItems.Add(bk);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return listItems;
        }

        public List<ListViewItem> popUsers(bool all = true)
        {
            SqlConnection connection;

            List<ListViewItem> listItems = new List<ListViewItem>();

            string query = "SELECT * FROM tblUsers";
            Debug.WriteLine(String.Format("Sending SQL command: {0}", query));

            try
            {
                using (connection = new SqlConnection(getCString()))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

                    DataTable dt = new DataTable();

                    adapter.Fill(dt);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow dr = dt.Rows[i];

                        string formattedName = String.Format("{0} {1}", dr["FirstName"], dr["SecondName"]);

                        string[] list = new string[] { dr["Id"].ToString(), dr["Username"].ToString(), formattedName, dr["Role"].ToString(), dr["Email"].ToString() };

                        ListViewItem li = new ListViewItem(list);

                        listItems.Add(li);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return listItems;
        }

        public loginReturnedData checkLoginDetails(string username, string pass)
        {
            loginReturnedData returnedData = new loginReturnedData();

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("SELECT * FROM tblUsers WHERE Username = '{0}' AND Password = '{1}'", username, pass);
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));

                SqlCommand logincommand = new SqlCommand(command, connection);
                SqlDataReader reader = logincommand.ExecuteReader();

                if (reader.Read())
                {
                    returnedData.success = true;
                    returnedData.userID = reader.GetInt32(0);
                    returnedData.username = reader.GetString(1);
                    returnedData.name = new string[] { reader.GetString(2), reader.GetString(3) };
                    returnedData.role = reader.GetString(5);
                    returnedData.email = reader.GetString(6);
                }
            }

            return returnedData;
        }

        public bool checkUsernameExists(string username, int editId)
        {
            bool exists = false;

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("SELECT Id FROM tblUsers WHERE Username = '{0}'", username);
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));

                SqlCommand logincommand = new SqlCommand(command, connection);

                int recievedId;

                // if username trying to change to is the user's current username
                try
                {
                    recievedId = (int)logincommand.ExecuteScalar();
                }
                catch
                {
                    return false;
                }

                if (recievedId == editId)
                {
                    return false;
                }

                // if username exists
                try
                {
                    exists = (int)logincommand.ExecuteScalar() > 0;
                }
                catch (NullReferenceException)
                {
                    exists = false;
                }

                return exists;
            }
        }

        public bool checkBookingExists(string date, int period, int room, int editId = -1)
        {
            try
            {
                bool exists = false;

                using (connection = new SqlConnection(getCString()))
                {
                    connection.Open();

                    string command = String.Format("SELECT Id FROM tblBookings WHERE Date = CONVERT(date, '{0}', 103) AND Period = {1} AND RoomID = {2}", date, period, room);
                    Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                    SqlCommand logincommand = new SqlCommand(command, connection);

                    int recievedId;

                    // if username trying to change to is the user's current username
                    try
                    {
                        recievedId = (int)logincommand.ExecuteScalar();
                    }
                    catch
                    {
                        return false;
                    }

                    if (editId != -1 && recievedId == editId)
                    {
                        return false;
                    }

                    // if username exists
                    try
                    {
                        exists = (int)logincommand.ExecuteScalar() > 0;
                        Debug.WriteLine(exists.ToString());
                    }
                    catch (NullReferenceException)
                    {
                        exists = false;
                    }

                    return exists;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        public booking getBooking(int id)
        {
            booking booking = new booking();

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("SELECT * FROM tblBookings WHERE Id = {0}", id.ToString());
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                SqlCommand logincommand = new SqlCommand(command, connection);
                SqlDataReader reader = logincommand.ExecuteReader();

                if (reader.Read())
                {
                    booking.id = reader.GetInt32(0);
                    booking.date = reader.GetDateTime(2);
                    booking.UserID = reader.GetInt32(4);
                    booking.period = reader.GetInt32(3);
                    booking.notes = reader.GetString(5);
                    booking.roomID = reader.GetInt32(1);
                }
            }

            return booking;
        }

        public user getUser(int id)
        {
            user user = new user();

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("SELECT * FROM tblUsers WHERE Id = {0}", id.ToString());
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                SqlCommand logincommand = new SqlCommand(command, connection);
                SqlDataReader reader = logincommand.ExecuteReader();

                if (reader.Read())
                {
                    user.id = reader.GetInt32(0);
                    user.username = reader.GetString(1);
                    user.firstname = reader.GetString(2);
                    user.secondname = reader.GetString(3);
                    user.password = reader.GetString(4);
                    user.role = reader.GetString(5);
                    user.email = reader.GetString(6);
                }
            }

            return user;
        }

        public void insertBooking(int roomID, DateTime date, int period, int userID, string notes)
        {
            DateTime dt = new DateTime();
            dt = DateTime.Now;

            string newdt = date.ToShortDateString();

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("INSERT INTO tblBookings (RoomID, Date, Period, UserID, Notes, TimeBooked) VALUES ({0}, CONVERT(date, '{1}', 103), {2}, {3}, '{4}', CONVERT(datetime, '{5}', 103))", roomID.ToString(), date, period, userID.ToString(), notes, dt.ToString());
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                SqlCommand logincommand = new SqlCommand(command, connection);
                try
                {
                    logincommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void updateBooking(int bookID, int roomID, DateTime date, int period, int userID, string notes)
        {
            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("UPDATE tblBookings SET RoomID = {0}, Date = CONVERT(date, '{1}', 103), Period = {2}, UserID = {3}, Notes = '{4}' WHERE Id = {5}", roomID.ToString(), date, period, userID.ToString(), notes, bookID.ToString());
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                SqlCommand logincommand = new SqlCommand(command, connection);
                try
                {
                    logincommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void updateUser(int id, string firstname, string secondname, string password, string email, string role, string username)
        {
            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = String.Format("UPDATE tblUsers SET FirstName = '{0}', SecondName = '{1}', Password = '{2}', Role='{4}', Email = '{3}', Username = '{6}'  WHERE Id = {5}", firstname, secondname, password, email, role, id.ToString(), username);
                Debug.WriteLine(String.Format("Sending SQL command: {0}", command));
                SqlCommand logincommand = new SqlCommand(command, connection);
                try
                {
                    logincommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void miscAction(string query)
        {
            Debug.WriteLine(String.Format("Sending SQL command: {0}", query));

            using (connection = new SqlConnection(getCString()))
            {
                connection.Open();

                string command = query;

                SqlCommand logincommand = new SqlCommand(command, connection);
                try
                {
                    logincommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}