using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class Sql
    {
        public delegate bool ConnectionEvent();
        public event ConnectionEvent OnConnection;

        string _cmdTect;
        bool _storedProcedure;

        List<SqlParameter> _spParameters;

        public Sql(string cmdText, bool storedProcedure)
        {
            _cmdTect = cmdText;
            _storedProcedure = storedProcedure;
        }

        public void AddParameter(SqlParameter paramter)
        {
            if (_spParameters == null)
            {
                _spParameters = new List<SqlParameter>();
            }
            _spParameters.Add(paramter);
        }

        public string CmdText
        {
            get { return _cmdTect; }
        }


        public SqlCommand Execute(SqlConnection conn)
        {
            SqlCommand cmd = null;
            try
            {
                // Only if no one cares or the result is true.
                if ((OnConnection == null) || OnConnection().Equals(true))
                {
                    cmd = new SqlCommand();
                    cmd.Connection = conn;
                    if (_storedProcedure)
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        foreach (SqlParameter param in _spParameters)
                        {
                            cmd.Parameters.Add(param);
                        }
                    }
                    else
                    {
                        cmd.CommandType = CommandType.Text;
                    }
                    cmd.CommandTimeout = 5;
                    cmd.CommandText = _cmdTect;

                    conn.Open();

                    return cmd;
                }
            }
            catch (SqlException ex)
            {
                Log.Error("MyMovies::GetSqlData - Error retrieving data for query '{0}'", _cmdTect);
                Log.Error(ex);
            }
            return null;
        }

        
        internal static string Connection(string serverName, string dbInstance, string userName, string password)
        {
            StringBuilder sqlConnection = new StringBuilder();
            sqlConnection.AppendFormat("Server={0}", serverName);
            sqlConnection.Append((dbInstance.Equals(string.Empty)) ? "; " : string.Format(@"\{0}; ", dbInstance));
            sqlConnection.Append("Database=My Movies; ");

            // Use trusted connections
            if (userName.Equals(string.Empty) || password.Equals(string.Empty))
            {
                sqlConnection.Append(string.Format(@"Trusted_Connection=True;"));
            }
            else
            {
                sqlConnection.Append(string.Format(@"User ID={0}; Password={1}; Trusted_Connection=False;", userName, password));
            }
            return sqlConnection.ToString();
        }
    }
}
