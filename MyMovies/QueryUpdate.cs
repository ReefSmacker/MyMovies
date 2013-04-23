using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class QueryUpdate : Sql
    {
        public QueryUpdate(string updateQuery)
         : base(updateQuery, false)
        { 
        }

        public int Execute(string connection)
        {
            int rowEffected = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    SqlCommand cmd = base.Execute(conn);
                    if (cmd != null)
                    {
                        rowEffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                Log.Error("QueryUpdate::Execute - Error retrieving data for query '{0}'", CmdText);
                Log.Error(ex);
            }
            return rowEffected;
        }
    }
}
