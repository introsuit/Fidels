using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Diagnostics;

namespace Fidels
{
    public struct WeeksRange
    {
        public int from;
        public int to;
    }

    class Service
    {
        private static Service service;
        private Dao dao = Dao.getInstance();

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        private Service()
        {

        }

        public WeeksRange getWeeksRange(int year, int month)
        {
            WeeksRange weeksR = new WeeksRange();

            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime dateTime = new DateTime(year, month, 1);
            System.Globalization.Calendar cal = dfi.Calendar;
            weeksR.from = cal.GetWeekOfYear(dateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            //special case
            if (month == 12)
            {
                dateTime = new DateTime(year, month, 31);
            }
            else
            {
                dateTime = new DateTime(year, month + 1, 1).AddDays(-1);
            }

            weeksR.to = cal.GetWeekOfYear(dateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);      

            return weeksR;
        }

        public bool ensureWeek()
        {
            bool inserted = false;
            DateTime now = DateTime.Now;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;
            int week = cal.GetWeekOfYear(now, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            SqlConnection con = dao.getConnection();

            SqlDataReader reader = null;
            try
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM stock " + "WHERE username = @username AND password = @password", con);
                //cmd.CommandText = "SELECT * FROM customer";
                //cmd.Parameters.Add(new SqlParameter("username", username));
                //cmd.Parameters.Add(new SqlParameter("password", password));
                con.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                }
            }
            finally
            {
                // 3. close the reader
                if (reader != null)
                {
                    reader.Close();
                }

                // close the connection
                if (con != null)
                {
                    con.Close();
                }
            }

            return inserted;
        }

        public DataTable getStocks(int year, int month, int weekNo)
        {
            SqlDataAdapter stocksAdapter = getStocksAdapter(dao.getConnection(), year, month);
            DataTable dt = getDataTable(stocksAdapter);
            DataTable newTable = dt.Copy();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;

                DateTime dateTime = dt.Rows[i].Field<DateTime>("date");
                System.Globalization.Calendar cal = dfi.Calendar;

                int actWeek = cal.GetWeekOfYear(dateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                if (actWeek != weekNo)
                {
                    newTable.Rows[i].Delete();
                }
            }

            return newTable;
        }

        private DataTable getDataTable(SqlDataAdapter adapter)
        {
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        private SqlDataAdapter getStocksAdapter(SqlConnection connection, int year, int month)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();

            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM stock JOIN product ON stock.product_id = product.product_id JOIN product_group ON product.product_group_id = product_group.product_group_id WHERE month(date) = @month AND year(date) = @year", connection);

            command.Parameters.Add(new SqlParameter("month", month));
            command.Parameters.Add(new SqlParameter("year", year));
            adapter.SelectCommand = command;

            return adapter;
        }
    }
}
