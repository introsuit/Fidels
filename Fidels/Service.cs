﻿using System;
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
        private SqlConnection connection = null;

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        private Service()
        {
            connection = dao.getConnection();
        }

        public WeeksRange getWeeksRange(int year, int month)
        {
            WeeksRange weeksR = new WeeksRange();
            DateTime dateTime = new DateTime(year, month, 1);
            weeksR.from = getWeek(dateTime);

            //special case
            if (month == 12)
            {
                dateTime = new DateTime(year, month, 31);
            }
            else
            {
                dateTime = new DateTime(year, month + 1, 1).AddDays(-1);
            }

            weeksR.to = getWeek(dateTime);

            return weeksR;
        }

        public int getWeek(DateTime date)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            System.Globalization.Calendar cal = dfi.Calendar;
            return cal.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
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
                DateTime dateTime = dt.Rows[i].Field<DateTime>("date");

                int actWeek = getWeek(dateTime);
                if (actWeek != weekNo)
                {
                    newTable.Rows[i].Delete();
                }
            }
            newTable.AcceptChanges();

            // ask to explain
            if (newTable.Rows.Count == 0)
            {
                if (weekNo > 1)
                {
                    stocksAdapter = getStocksAdapter(dao.getConnection(), year, month);
                    dt = getDataTable(stocksAdapter);
                    newTable = dt.Copy();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DateTime dateTime = dt.Rows[i].Field<DateTime>("date");
                        int actWeek = getWeek(dateTime);
                        //why weekNo - 1
                        if (actWeek != weekNo - 1)
                        {
                            newTable.Rows[i].Delete();
                        }
                    }
                    newTable.AcceptChanges();
                }
                else if (weekNo == 1)
                {
                    //change
                    stocksAdapter = getStocksAdapter(dao.getConnection(), year - 1, 12);
                    dt = getDataTable(stocksAdapter);
                    newTable = dt.Copy();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DateTime dateTime = dt.Rows[i].Field<DateTime>("date");
                        int actWeek = getWeek(dateTime);
                        //change
                        if (actWeek != 53)
                        {
                            newTable.Rows[i].Delete();
                        }
                    }
                    newTable.AcceptChanges();
                }

                // if table is empty, it populates table with default items which date is 2001-01-01
                if (newTable.Rows.Count == 0)
                {
                    stocksAdapter = getDefaultStocksAdapter(dao.getConnection());
                    newTable = getDataTable(stocksAdapter);
                }
            }

            return newTable;
        }

        public DataTable getFakturas(int year, int month, int weekNo)
        {
            SqlDataAdapter fakturasAdapter = getFakturasAdapter(dao.getConnection(), year, month);
            DataTable dt = getDataTable(fakturasAdapter);
            DataTable newTable = dt.Copy();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DateTime dateTime = dt.Rows[i].Field<DateTime>("date");

                int actWeek = getWeek(dateTime);
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

        private SqlDataAdapter getDefaultStocksAdapter(SqlConnection connection)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();

            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM stock JOIN product ON stock.product_id = product.product_id JOIN product_group ON product.product_group_id = product_group.product_group_id WHERE date = '2001-01-01'", connection);

            adapter.SelectCommand = command;

            return adapter;
        }

        private SqlDataAdapter getStocksAdapter(SqlConnection connection, int year, int month)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();

            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM stock JOIN product ON stock.product_id = product.product_id JOIN product_group ON product.product_group_id = product_group.product_group_id WHERE month(date) = @month AND year(date) = @year", connection);

            command.Parameters.Add(new SqlParameter("month", month));
            command.Parameters.Add(new SqlParameter("year", year));
            adapter.SelectCommand = command;

            command = new SqlCommand(
               "UPDATE stock SET unit_price = @unit_price, speed_rail = @speed_rail, stock_bar = @stock_bar, " + "display = @display, office_stock = @office_stock, min_stock = @min_stock, date = @date, delivery = @delivery " +
               "WHERE stock_id = @stock_id", connection);

            // Add the parameters for the UpdateCommand.
            command.Parameters.Add("@unit_price", SqlDbType.Decimal, 2, "unit_price");
            command.Parameters.Add("@speed_rail", SqlDbType.Int, 2, "speed_rail");
            command.Parameters.Add("@stock_bar", SqlDbType.Int, 2, "stock_bar");
            command.Parameters.Add("@display", SqlDbType.Int, 2, "display");
            command.Parameters.Add("@office_stock", SqlDbType.Int, 2, "office_stock");
            command.Parameters.Add("@min_stock", SqlDbType.Int, 2, "min_stock");
            command.Parameters.Add("@date", SqlDbType.Date, 2, "date");
            command.Parameters.Add("@delivery", SqlDbType.Int, 2, "delivery");
            //command.Parameters.Add(new SqlParameter("unit_price", "@unit_price"));
            // command.Parameters.Add("@changeover_day", SqlDbType.VarChar, 50, "changeover_day");
            //command.Parameters.Add("@flight_price", SqlDbType.Decimal, 2, "flight_price");
            SqlParameter parameter = command.Parameters.Add(
                "@stock_id", SqlDbType.Int, 5, "stock_id");
            parameter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = command;

            return adapter;
        }

        private SqlDataAdapter getFakturasAdapter(SqlConnection connection, int year, int month)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();

            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM faktura JOIN company ON faktura.company_id = company.company_id WHERE month(date) = @month AND year(date) = @year", connection);

            command.Parameters.Add(new SqlParameter("month", month));
            command.Parameters.Add(new SqlParameter("year", year));
            adapter.SelectCommand = command;

            return adapter;
        }

        public void updateStocks(DataTable dataTable)
        {
            SqlDataAdapter adapter = getStocksAdapter(connection, 0, 0);

            adapter.Update(dataTable);
        }
    }
}
