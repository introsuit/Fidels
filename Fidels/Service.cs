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
        public int from { get; set; }
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

        // returns stocks from database for set date
        // if stocks is empty, creates new stocks from default values
        // or creates new stocks from copy of last weeks values
        public DataTable getStocks(int year, int month, int weekNo)
        {
            DataTable dataTable = filterDataTable(year, month, weekNo);
            
            //if there is no weeks in database for this date, it checks if returned datatable contains
            //0 rows and selected week in combobox is exactly the same as DateTime.Now
            if (dataTable.Rows.Count == 0 && getWeek(DateTime.Now) == weekNo)
            {
                if (weekNo > 1)
                {                  
                    // if its first week of the month
                    // it takes copy of previous month's last week products from database
                    // changes its dates to DateTime.Now
                    // inserts products back to database with new date. (not working)
                    if (getWeeksRange(year, month).from == weekNo)
                    {
                        dataTable = filterDataTable(year, month - 1, weekNo - 1);
                        dataTable = changeDate(dataTable);
                        
                    }
                    // it takes copy of last week products from database
                    // changes its dates to DateTime.Now
                    // inserts products back to database with new date. (not working)
                    else
                    {
                        dataTable = filterDataTable(year, month, weekNo - 1);                       
                        dataTable = changeDate(dataTable);
                    }
                }
                else if (weekNo == 1)
                {
                    dataTable = filterDataTable(year - 1, 12, 53);
                    dataTable = changeDate(dataTable);
                }

                // if table is still empty, it takes copy of default products from database
                // changes its dates to DateTime.Now
                // inserts products back to database with new date. (not working)
                if (dataTable.Rows.Count == 0)
                {
                    SqlDataAdapter stocksAdapter = getDefaultStocksAdapter(dao.getConnection());
                    dataTable = getDataTable(stocksAdapter);
                    dataTable = changeDate(dataTable);
                }
            }

            return dataTable;
        }

        //changing products in database date to DateTime.Now
        public DataTable changeDate(DataTable dataTable)
        {
            DataTable newTable = dataTable.Copy();
            for (int i = 0; i < newTable.Rows.Count; i++)
            {
                DataColumn col = newTable.Columns["date"];
                newTable.Rows[i].SetField(col, DateTime.Now);
            }
            newTable.AcceptChanges();
            Debug.WriteLine("count: " +newTable.Rows.Count);
            updateStocks(newTable); //inserts products datatable back to database with new date. (not working)
            return newTable;
        }

        //taking stocks datatable from database and sorting it on week to return products on selected week
        public DataTable filterDataTable(int year, int month, int week)
        {
            SqlDataAdapter stocksAdapter = getStocksAdapter(dao.getConnection(), year, month);
            DataTable dataTable = getDataTable(stocksAdapter);
            DataTable newTable = dataTable.Copy();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DateTime dateTime = dataTable.Rows[i].Field<DateTime>("date");
                int actWeek = getWeek(dateTime);
                if (actWeek != week)
                {
                    newTable.Rows[i].Delete();
                }
            }
            newTable.AcceptChanges();
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
            newTable.AcceptChanges();
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

            SqlParameter parameter = command.Parameters.Add(
                "@stock_id", SqlDbType.Int, 5, "stock_id");
            parameter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = command;

            command = new SqlCommand("INSERT INTO stock VALUES (@product_id, @unit_price, @speed_rail, @stock_bar, @display, @office_stock, @min_stock, @date, @delivery)", connection);

            // Add the parameters for the InsertCommand.
            command.Parameters.Add("@product_id", SqlDbType.Int, 2, "product_id");
            command.Parameters.Add("@unit_price", SqlDbType.Decimal, 2, "unit_price");
            command.Parameters.Add("@speed_rail", SqlDbType.Int, 2, "speed_rail");
            command.Parameters.Add("@stock_bar", SqlDbType.Int, 2, "stock_bar");
            command.Parameters.Add("@display", SqlDbType.Int, 2, "display");
            command.Parameters.Add("@office_stock", SqlDbType.Int, 2, "office_stock");
            command.Parameters.Add("@min_stock", SqlDbType.Int, 2, "min_stock");
            command.Parameters.Add("@date", SqlDbType.Date, 2, "date");
            command.Parameters.Add("@delivery", SqlDbType.Int, 2, "delivery");

            adapter.InsertCommand = command;

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

        public static String orderPrint(DataTable data)
        {
            string str = "";
            int i = -1;
            foreach (DataRow row in data.Rows)
            {
                i++;
                int total = (row.Field<int>("speed_rail") +
                    row.Field<int>("stock_bar") +
                    row.Field<int>("display") +
                    row.Field<int>("office_stock"));

                if (total < row.Field<int>("min_stock"))
                {
                    str = str + "Name: " + row.Field<string>("name") + "  | Amount to buy: " + (row.Field<int>("min_stock") - total).ToString() + "\r\n";

                }
            }
            return str;
        }
    }
}
