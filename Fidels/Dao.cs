using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Fidels
{
    class Dao
    {
        private static Dao dao;

        public static Dao getInstance()
        {
            if (dao == null)
                dao = new Dao();
            return dao;
        }

        private Dao()
        {

        }

        string conStr = @"Data Source=localhost\SQLExpress; database=Fidels; Integrated Security=true;";
        SqlConnection con = null;

        public SqlConnection getConnection()
        {
            if (con == null)
                con = new SqlConnection(conStr);

            return con;
        }

        public bool CheckConnection()
        {
            SqlConnection con = getConnection();
            try
            {
                con.Open();
                con.Close();
                return true;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}
