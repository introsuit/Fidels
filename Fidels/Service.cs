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
        private int supposedWagePercent = 20;
        private int supposedFakturaPercent = 5;

        public int getSupposedWagePercent() {
            return supposedWagePercent;
        }

        public int getSupposedFakturaPercent() {
            return supposedFakturaPercent;
        }

        public void setSupposedWagePercent(int i)
        {
            supposedWagePercent = i;
        }

        public void setSupposedFakturaPercent(int i)
        {
            supposedFakturaPercent = i;
        }

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

        public DataTable getStocks(int year, int month, int weekNo)
        {
            DataTable dataTable = filterDataTable(year, month, weekNo);

            if (dataTable.Rows.Count == 0 && getWeeksRange(year, month).from == weekNo)
                dataTable = filterDataTable(year, month - 1, weekNo);

            if (dataTable.Rows.Count == 0 && getWeek(DateTime.Now) == weekNo)
            {
                if (weekNo > 1)
                    if (getWeeksRange(year, month).from == weekNo)
                        dataTable = filterDataTable(year, month - 1, weekNo - 1);
                    else
                        dataTable = filterDataTable(year, month, weekNo - 1);
                else if (weekNo == 1)
                    dataTable = filterDataTable(year - 1, 12, 53);
                if (dataTable.Rows.Count == 0)
                {
                    SqlDataAdapter stocksAdapter = getDefaultStocksAdapter(dao.getConnection());
                    dataTable = getDataTable(stocksAdapter);
                }
                changeDate(dataTable);
                dataTable = filterDataTable(year, month, weekNo);
            }
            return dataTable;
        }

        //changing products in database date to DateTime.Now
        public void changeDate(DataTable dataTable)
        {
            DataTable newTable = dataTable.Copy();
            for (int i = 0; i < newTable.Rows.Count; i++)
            {
                DataColumn col = newTable.Columns["date"];
                newTable.Rows[i].SetField(col, DateTime.Now);
            }
            newTable.AcceptChanges();
            createStocks(newTable);
            dataTable = newTable;
        }

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

            SqlDataAdapter fakturasAdapter = getFakturasAdapter(dao.getConnection(), year, weekNo);
            DataTable dataTable = getDataTable(fakturasAdapter);
            return dataTable;
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

        private SqlDataAdapter getEmployeesHoursAdapter(int year, int weekNo)
        {
            DateTime firstDayOfWeek = firstDateOfWeek(year, weekNo, CultureInfo.CurrentCulture);
            SqlDataAdapter adapter = new SqlDataAdapter();

            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM employee_hours JOIN employee ON employee_hours.employee_id = employee.employee_id WHERE date >= @first AND date <= @last", connection);
            command.Parameters.AddWithValue("first", firstDayOfWeek);
            command.Parameters.AddWithValue("last", firstDayOfWeek.AddDays(6));
            adapter.SelectCommand = command;
            return adapter;
        }

        public bool updateEmployee(string name, decimal hourlyWage, TimeSpan time, int employeeId, int employeeHoursId)
        {
            int result = 0;
            connection.Open();
            SqlCommand command = new SqlCommand("UPDATE employee SET name = @name, hourly_wage = @hourly_wage WHERE employee_id = @employee_id", connection);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("hourly_wage", hourlyWage);
            command.Parameters.AddWithValue("employee_id", employeeId);
            result += command.ExecuteNonQuery();
            command = new SqlCommand("UPDATE employee_hours SET worked_hours = @worked_hours WHERE employee_hours_id = @employee_hours_id", connection);
            command.Parameters.AddWithValue("worked_hours", time);
            command.Parameters.AddWithValue("employee_hours_id", employeeHoursId);
            result += command.ExecuteNonQuery();
            connection.Close();
            return result > 0;
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

            return adapter;
        }

        private DateTime firstDateOfWeek(int year, int weekOfYear, System.Globalization.CultureInfo ci)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            DateTime firstWeekDay = jan1.AddDays(daysOffset);
            int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            if (firstWeek <= 1 || firstWeek > 50)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }

        public DataTable getEmployeesHours(int year, int weekNo)
        {
            SqlDataAdapter emplAdapter = getEmployeesHoursAdapter(year, weekNo);
            DataTable dataTable = getDataTable(emplAdapter);

            if (dataTable.Rows.Count == 0 && getWeek(DateTime.Now) == weekNo)
            {
                insertEmployeesHours(year, weekNo);
            }

            return getDataTable(emplAdapter);
        }

        private void insertEmployeesHours(int year, int weekNo)
        {
            connection.Open();
            SqlCommand command = new SqlCommand("SELECT employee_id FROM employee", connection);
            SqlDataReader sqlReader = command.ExecuteReader();
            List<int> employees = new List<int>();
            while (sqlReader.Read())
            {
                int employee_id = Convert.ToInt32(sqlReader["employee_id"]);
                employees.Add(employee_id);
            }
            connection.Close();

            connection.Open();
            DateTime now = DateTime.Now;
            foreach (int employee_id in employees)
            {
                command = new SqlCommand("INSERT INTO employee_hours VALUES (@employee_id, @worked_hours, @date)", connection);
                command.Parameters.AddWithValue("employee_id", employee_id);

                command.Parameters.AddWithValue("worked_hours", new TimeSpan());
                command.Parameters.AddWithValue("date", now);
                command.ExecuteNonQuery();
            }
            connection.Close();
        }

        //enter current week. will return how many bottles
        //sold from previous week
        public DataTable bottlesSold(int year, int weekNo)
        {
            DateTime firstDayOfWeek = firstDateOfWeek(year, weekNo, CultureInfo.CurrentCulture);
            firstDayOfWeek = firstDayOfWeek.AddDays(-7); //-1 week

            SqlDataAdapter adapter = new SqlDataAdapter();
            SqlCommand command = new SqlCommand("SELECT * FROM stock WHERE date >= @first AND date <= @last", connection);
            command.Parameters.AddWithValue("first", firstDayOfWeek);
            command.Parameters.AddWithValue("last", firstDayOfWeek.AddDays(6));
            adapter.SelectCommand = command;

            return getDataTable(adapter);
        }

        public bool deleteProduct(int product_id)
        {
            connection.Open();
            int result = 0;

            //deleting from defaults
            SqlCommand command = new SqlCommand("DELETE FROM stock WHERE product_id = @product_id AND date = '2001-01-01'", connection);
            command.Parameters.AddWithValue("product_id", product_id);
            result += command.ExecuteNonQuery();

            //deleting from today
            DateTime now = DateTime.Now;
            DateTime firstDayOfWeek = firstDateOfWeek(now.Year, getWeek(now), CultureInfo.CurrentCulture);

            command = new SqlCommand("DELETE FROM stock WHERE product_id = @product_id AND (date >= @first AND date <= @last)", connection);
            command.Parameters.AddWithValue("product_id", product_id);
            command.Parameters.AddWithValue("first", firstDayOfWeek);
            command.Parameters.AddWithValue("last", firstDayOfWeek.AddDays(6));
            result += command.ExecuteNonQuery();

            connection.Close();
            return result > 0;
        }

        public bool deleteEmployee(int employee_id)
        {
            connection.Open();
            int result = 0;

            //deleting from defaults
            SqlCommand command = new SqlCommand("UPDATE employee SET active = 0 WHERE employee_id = @employee_id", connection);
            command.Parameters.AddWithValue("employee_id", employee_id);
            command.ExecuteNonQuery();

            //deleting from today
            DateTime now = DateTime.Now;
            DateTime firstDayOfWeek = firstDateOfWeek(now.Year, getWeek(now), CultureInfo.CurrentCulture);

            command = new SqlCommand("DELETE FROM employee_hours WHERE employee_id = @employee_id AND (date >= @first AND date <= @last)", connection);
            command.Parameters.AddWithValue("employee_id", employee_id);
            command.Parameters.AddWithValue("first", firstDayOfWeek);
            command.Parameters.AddWithValue("last", firstDayOfWeek.AddDays(6));
            result += command.ExecuteNonQuery();

            connection.Close();
            return result > 0;
        }

        public void createStocks(DataTable dataTable)
        {
            connection.Open();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                SqlCommand command = new SqlCommand("INSERT INTO stock VALUES (@product_id, @unit_price, @speed_rail, @stock_bar, @display, @office_stock, @min_stock, @date, @delivery)", connection);
                command.Parameters.AddWithValue("product_id", dataTable.Rows[i].Field<int>("product_id"));
                command.Parameters.AddWithValue("unit_price", dataTable.Rows[i].Field<decimal>("unit_price"));
                command.Parameters.AddWithValue("speed_rail", dataTable.Rows[i].Field<int>("speed_rail"));
                command.Parameters.AddWithValue("stock_bar", dataTable.Rows[i].Field<int>("stock_bar"));
                command.Parameters.AddWithValue("display", dataTable.Rows[i].Field<int>("display"));
                command.Parameters.AddWithValue("office_stock", dataTable.Rows[i].Field<int>("office_stock"));
                command.Parameters.AddWithValue("min_stock", dataTable.Rows[i].Field<int>("min_stock"));
                command.Parameters.AddWithValue("date", dataTable.Rows[i].Field<DateTime>("date"));
                command.Parameters.AddWithValue("delivery", dataTable.Rows[i].Field<int>("delivery"));
                command.ExecuteNonQuery();
            }
            connection.Close();
        }

        public List<string> updateComboBox()
        {
            List<string> list = new List<string>();
            connection.Open();
            SqlCommand sqlCmd = new SqlCommand("SELECT * FROM product_group", connection);
            SqlDataReader sqlReader = sqlCmd.ExecuteReader();
            while (sqlReader.Read())
            {
                list.Add(sqlReader["product_name"].ToString());
            }
            sqlReader.Close();
            connection.Close();
            return list;
        }

        public void createProduct(string name, int productGroupId, decimal unitPrice, int speedRail, int stockBar, int display, int officeStock, int minimumStock)
        {
            connection.Open();
            SqlCommand command = new SqlCommand("INSERT INTO product VALUES (@name, @product_group_id);SELECT CAST(scope_identity() AS int)", connection);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("product_group_id", productGroupId);
            int productId = (int)command.ExecuteScalar();
            DateTime dateTime = DateTime.Now;
            for (int i = 0; i < 2; i++)
            {
                command = new SqlCommand("INSERT INTO stock VALUES (@product_id, @unit_price, @speed_rail, @stock_bar, @display, @office_stock, @min_stock, @date, @delivery)", connection);
                command.Parameters.AddWithValue("product_id", productId);
                command.Parameters.AddWithValue("unit_price", unitPrice);
                command.Parameters.AddWithValue("speed_rail", speedRail);
                command.Parameters.AddWithValue("stock_bar", stockBar);
                command.Parameters.AddWithValue("display", display);
                command.Parameters.AddWithValue("office_stock", officeStock);
                command.Parameters.AddWithValue("min_stock", minimumStock);
                command.Parameters.AddWithValue("date", dateTime);
                command.Parameters.AddWithValue("delivery", 0);
                command.ExecuteNonQuery();
                unitPrice = 0;
                speedRail = 0;
                stockBar = 0;
                display = 0;
                officeStock = 0;
                minimumStock = 0;
                dateTime = new DateTime(2001, 1, 1);
            }
            connection.Close();
        }

        private SqlDataAdapter getFakturasAdapter(SqlConnection connection, int year, int weekNo)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DateTime firstDayOfWeek = firstDateOfWeek(year, weekNo, CultureInfo.CurrentCulture);
            // Create the SelectCommand.
            SqlCommand command = new SqlCommand("SELECT * FROM faktura JOIN company ON faktura.company_id = company.company_id WHERE date >= @first AND date <= @last", connection);
            command.Parameters.AddWithValue("first", firstDayOfWeek);
            command.Parameters.AddWithValue("last", firstDayOfWeek.AddDays(6));
            adapter.SelectCommand = command;
            return adapter;
        }

        public DataTable getFakturas()
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            SqlCommand command = new SqlCommand("SELECT * FROM faktura", connection);
            adapter.SelectCommand = command;
            DataTable dtFakturaList = new DataTable();
            adapter.Fill(dtFakturaList);
            Debug.WriteLine(dtFakturaList.Rows.Count);
            return dtFakturaList;
        }

        public List<string> getCompanyNames()
        {
            List<string> list = new List<string>();
            connection.Open();
            SqlCommand sqlCmd = new SqlCommand("SELECT * FROM company", connection);
            SqlDataReader sqlReader = sqlCmd.ExecuteReader();
            while (sqlReader.Read())
            {
                list.Add(sqlReader["name"].ToString());
            }
            sqlReader.Close();
            connection.Close();
            return list;
        }

        public void AddFaktura(int company_id, string serial_no, decimal price)
        {
            DateTime now = DateTime.Now;
            SqlCommand command = new SqlCommand("INSERT INTO faktura VALUES (@company_id, @serial_no, @date, @price)", connection);
            command.Parameters.Add(new SqlParameter("company_id", company_id));
            command.Parameters.Add(new SqlParameter("serial_no", serial_no));
            command.Parameters.Add(new SqlParameter("date", now));
            command.Parameters.Add(new SqlParameter("price", price));
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        public void deleteFaktura(int faktura_id)
        {
            SqlCommand command = new SqlCommand("DELETE FROM faktura WHERE faktura_id=@faktura_id", connection);
            command.Parameters.Add(new SqlParameter("faktura_id", faktura_id));
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();

        }

        public int getTotalWeeklyFakturaAmount(int year, int month, int weekNo)
        {
            int i = 0;
            DataTable dt = getFakturas(year, month, weekNo);
            foreach (DataRow row in dt.Rows) {
                i = i + Convert.ToInt32(row["price"]);
            }
            return i;
        }
            
        public void updateStocks(DataTable dataTable)
        {
            SqlDataAdapter adapter = getStocksAdapter(connection, 0, 0);
            adapter.Update(dataTable);
        }

        public String orderPrint(DataTable data)
        {
            string str = "";
            foreach (DataRow row in data.Rows)
            {
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

        public bool createEmployee(string name, TimeSpan time, decimal hourlyWage)
        {
            connection.Open();
            SqlCommand command = new SqlCommand("INSERT INTO employee VALUES (@name, @hourly_wage, 1);SELECT CAST(scope_identity() AS int)", connection);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("hourly_wage", hourlyWage);
            int employeeId = (int)command.ExecuteScalar();
            command = new SqlCommand("INSERT INTO employee_hours VALUES (@employee_id, @worked_hours, @date)", connection);
            command.Parameters.AddWithValue("employee_id", employeeId);
            command.Parameters.AddWithValue("worked_hours", time);
            command.Parameters.AddWithValue("date", DateTime.Now);
            int result = command.ExecuteNonQuery();
            connection.Close();

            return result > 0;
        }

        private int getTotalStock(DataRow dataRow)
        {
            int totalStock = (dataRow.Field<int>("speed_rail") +
                        dataRow.Field<int>("stock_bar") +
                        dataRow.Field<int>("display") +
                        dataRow.Field<int>("office_stock") +
                        dataRow.Field<int>("delivery"));
            return totalStock;
        }

        public decimal getPercentage(decimal p1, decimal p2) {
            decimal i = -1;
            if (p1 != 0)
            {
                i = p2 / (p1 / 100);
                return i;
            }
            else return -1;
        }

        // for budget calculations
        public void getStockTotals(int year, int month, int week, out decimal totalSupposeTurnOver, out decimal totalWeekStockValue)
        {
            DataTable thisWeekDataTable = filterDataTable(year, month, week);
            DataTable previousWeekDataTable = service.bottlesSold(year, week);
            totalSupposeTurnOver = 0;
            totalWeekStockValue = 0;
            foreach (DataRow thisWeekRow in thisWeekDataTable.Rows)
            {
                int bottlesSold = 0;
                decimal supposeTurnover = 0;
                decimal totalStockValue = 0;
                int thisWeekProductId = thisWeekRow.Field<int>("product_id");
                foreach (DataRow previousWeekRow in previousWeekDataTable.Rows)
                {
                    int previousWeekProductId = previousWeekRow.Field<int>("product_id");
                    if (thisWeekProductId == previousWeekProductId)
                    {
                        int thisWeekTotalStock = getTotalStock(thisWeekRow);
                        int previousWeekTotalStock = getTotalStock(previousWeekRow);
                        bottlesSold = previousWeekTotalStock - thisWeekTotalStock;
                        supposeTurnover = ((decimal.Divide(bottlesSold, 5) * 4 * 16) + (decimal.Divide(bottlesSold, 5) * 8)) * thisWeekRow.Field<decimal>("unit_price");
                        totalStockValue = thisWeekTotalStock * thisWeekRow.Field<decimal>("unit_price");
                    }
                }
                totalSupposeTurnOver += supposeTurnover;
                totalWeekStockValue += totalStockValue;
            }
        }

        // for budget calculations
        public decimal getTotalWagesCost(int year, int week)
        {
            decimal totalWagesCost = 0;
            DataTable employees = getEmployeesHours(year, week);
            foreach (DataRow row in employees.Rows)
            {
                TimeSpan workedHours = row.Field<TimeSpan>("worked_hours");
                decimal hourlyWage = row.Field<decimal>("hourly_wage");
                totalWagesCost += Decimal.Divide(hourlyWage, 3600) * (decimal)workedHours.TotalSeconds;
            }
            return totalWagesCost;
        }
    }
}
