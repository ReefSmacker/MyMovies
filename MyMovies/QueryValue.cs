using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class QueryValue : Sql
    {
        public QueryValue(string updateQuery)
         : base(updateQuery, false)
        { 
        }

        public object Execute(string connection)
        {
            object value = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    SqlCommand cmd = base.Execute(conn);
                    if (cmd != null)
                    {
                        value = cmd.ExecuteScalar();
                    }
                }
            }
            catch (SqlException ex)
            {
                Log.Error("QueryValue::Execute - Error retrieving data for query '{0}'", CmdText);
                Log.Error(ex);
            }
            return value;
        }
    }
}
