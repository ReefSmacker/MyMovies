using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using MediaPortal.GUI.Library;
using System.Data.SqlClient;
using System.Data;

namespace MyMovies
{
    public class QueryReader<T> : Sql
    {
        public delegate void Row(SqlDataReader row, T itemlist);
        public event Row OnRow;


        /// <summary>
        /// Create a query for either a query string of a stored procedure.
        /// </summary>
        /// <param name="query">query string or stored procedure name.</param>
        /// <param name="storedProcedure">bool to indicate if the query is a stored procedure or query string.</param>
        public QueryReader(string query)
            : base(query, false)
        {
        }

        public string QueryString
        {
            get { return CmdText; }
        }


        public bool Execute(string connection, T list)
        {
            bool status = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    SqlCommand cmd = base.Execute(conn);
                    if (cmd != null)
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (OnRow != null)
                                {
                                    OnRow(reader, list);
                                }
                            }
                        }
                        status = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                Log.Error("QueryReader::Execute - Error retrieving data for query '{0}'", CmdText);
                Log.Error(ex);
            }
            return status;
        }

    }
}
