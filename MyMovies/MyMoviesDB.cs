using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;
using System.IO;

namespace MyMovies
{
    public class MyMoviesDB
    {
        #region Query Strings
        private const string _setStopTime =
@"IF EXISTS('SELECT * FROM tblResume WHERE intId={0} AND intUserId={1}')
    UPDATE resume SET intStopTime={2}, datLastPlayed=GETDATE() WHERE intId={0} AND intUserId={1}
ELSE
    INSERT INTO tblResume (intId, intUserId, intStopTime, datLastPlayed) values({0},{1},{2},GETDATE())";

        #endregion

        string _connection;

        public MyMoviesDB(string connection)
        {
            _connection = connection;
        }

        public void SetMovieWatched(int movieId, bool yes)
        {
            try
            {
                QueryUpdate update = new QueryUpdate(string.Format("UPDATE tblTitlePersonal SET bitWatched = {0} WHERE intTitle = {1}", (yes) ? 1 : 0, movieId));
                update.Execute(_connection);
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::SetMovieWatched - unable to set watched status");
                Log.Error(ex);
            }
        }

        public int GetMovieStopTime(int movieId, int userId)
        {
            try
            {
                QueryValue getStopTime = new QueryValue(string.Format("SELECT * FROM tblResume WHERE intId={0} AND intUserId={1}", movieId, userId));
                return Convert.ToInt32(getStopTime.Execute(_connection));

            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::GetMovieStopTime - exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
            return 0;
        }

        public void SetMovieStopTime(int movieId, int userId, int stoptime)
        {
            try
            {
                QueryUpdate setStopTime = new QueryUpdate(String.Format(_setStopTime, movieId, userId, stoptime));
                setStopTime.Execute(_connection);
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::SetMovieStopTime - exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
        }

        public byte[] FromHexString(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hexString.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return bytes;
        }

        public string ToHexString(byte[] bytes)
        {
            char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }
            return new string(chars);
        }

        internal class StopTimeResumeData
        {
            public int stopTime;
            public string resumeString;
            public string fileName;
        }

        public int GetMovieStopTimeAndResumeData(int movieId, int userId, out string fileName, out byte[] resumeData)
        {
            // Only compare the file as only the the path is not stored.
            resumeData = null;
            StopTimeResumeData stopTimeResumeData = new StopTimeResumeData();
            stopTimeResumeData.stopTime = -1;
            stopTimeResumeData.fileName = string.Empty;

            try
            {
                QueryReader<StopTimeResumeData> resume = new QueryReader<StopTimeResumeData>(string.Format("SELECT * FROM tblResume where intId={0} AND intUserId={1}", movieId, userId));
                resume.OnRow += new QueryReader<StopTimeResumeData>.Row(ResumeData_OnRow);
                resume.Execute(_connection, stopTimeResumeData);

                if (stopTimeResumeData.stopTime != 0)
                {
                    resumeData = new byte[stopTimeResumeData.resumeString.Length / 2];
                    FromHexString(stopTimeResumeData.resumeString).CopyTo(resumeData, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::GetMovieStopTimeAndResumeData - exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
            fileName = stopTimeResumeData.fileName;

            return stopTimeResumeData.stopTime;
        }

        /// <summary>
        /// This only expects a single row. Anymore is an error.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="itemlist"></param>
        private void ResumeData_OnRow(System.Data.SqlClient.SqlDataReader row, StopTimeResumeData rowData)
        {
            if (rowData.stopTime != -1)
            {
                throw new ApplicationException("Too many rows returned");
            }

            rowData.stopTime = Convert.ToInt32(row["intStopTime"]);
            rowData.resumeString = Convert.ToString(row["nvcResumeData"]);
            rowData.fileName = Convert.ToString(row["nvcFileName"]);
        }

        public void SetMovieStopTimeAndResumeData(int movieId, int userId, int stoptime, byte[] resumeData, string pathName)
        {
            try
            {
                // Only store the file not the path as the next watch may be from a different machine.
                // Delimit the SQL string quotes.
                string fileName = Path.GetFileName(pathName).Replace("'", "''");
                string resumeString = "-";
                if (resumeData != null) resumeString = ToHexString(resumeData);

                string query;
                QueryValue exists = new QueryValue(String.Format("SELECT count(*) FROM tblResume WHERE intId={0} AND intUserId={1}", movieId, userId));
                if (Convert.ToInt32(exists.Execute(_connection)) == 0)
                {
                    query = String.Format("INSERT INTO tblResume (intId, intUserId, intStopTime, nvcResumeData, nvcFileName, datLastPlayed) VALUES({0},{1},{2},'{3}', '{4}', GETDATE())", movieId, userId, stoptime, resumeString, fileName);
                }
                else
                {
                    query = String.Format("UPDATE tblResume SET intStopTime={0}, nvcResumeData='{1}', nvcFileName='{2}', datLastPlayed=GETDATE() WHERE intId={3} AND intUserId={4}", stoptime, resumeString, fileName, movieId, userId);
                }
                QueryUpdate update = new QueryUpdate(query);
                update.Execute(_connection);
            }
            catch (Exception ex)
            {
                Log.Error("videodatabase exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
        }

    }
}
