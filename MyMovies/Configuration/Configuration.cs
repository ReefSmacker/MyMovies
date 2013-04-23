using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MediaPortal.ServiceImplementations;
using MediaPortal.Configuration;
using WakeOnLan;
using System.Net;
using System.Data.SqlClient;
using System.Xml;
using System.IO;

namespace MyMovies
{
    public partial class Configuration : Form
    {
        public Configuration()
        {
            InitializeComponent();
        }

        private void Configuration_Load(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(txtDBInstance, "Specifiy the database instance used");
            toolTip1.SetToolTip(txtServerName, "Machine name or IP address where the My Movies database resides");
            toolTip1.SetToolTip(txtUserName, "Leave blank to use trusted connection");
            toolTip1.SetToolTip(txtPassword, "Leave blank to use trusted connection");

            try
            {
                using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    DriveReplacements pathReplacements;

                    txtProgramDataPath.Text         = xmlreader.GetValueAsString("MyMovies", "txtProgramDataPath", @"C:\ProgramData\My Movies");
                    txtServerName.Text              = xmlreader.GetValueAsString("MyMovies", "txtServerName", "localhost");
                    txtDBInstance.Text              = xmlreader.GetValueAsString("MyMovies", "txtDBInstance", "MYMOVIES");
                    txtUserName.Text                = xmlreader.GetValueAsString("MyMovies", "txtUserName", string.Empty);
                    txtPassword.Text                = xmlreader.GetValueAsString("MyMovies", "txtPassword", string.Empty);
                    txtPINCode.Text                 = xmlreader.GetValueAsString("MyMovies", "txtPINCode", "4321");
                    chkEnableRemoteWakeup.Checked   = xmlreader.GetValueAsBool("MyMovies", "chkRemoteWakeup", false);

                    string xml = xmlreader.GetValueAsString("MyMovies", "xmlPathReplacement", string.Empty);
                    pathReplacements = new DriveReplacements(xml);
                    LoadGrid(pathReplacements);

                    Users users = new Users(xmlreader.GetValueAsString("MyMovies", "xmlUsers", string.Empty));
                    foreach (string user in users.Collection)
                    {
                        gridUsers.Rows.Add(new string[] { user });
                    }

                    MacAddress macAddress = MacAddress.Parse(xmlreader.GetValueAsString("MyMovies", "MACAddress", "00-00-00-00-00-00"));
                    numUDMac1.Value = macAddress[0];
                    numUDMac2.Value = macAddress[1];
                    numUDMac3.Value = macAddress[2];
                    numUDMac4.Value = macAddress[3];
                    numUDMac5.Value = macAddress[4];
                    numUDMac6.Value = macAddress[5];

                    IPAddress ipAddress = IPAddress.Parse(xmlreader.GetValueAsString("MyMovies", "IPAddress", "0.0.0.0"));
                    numUDIP1.Value = ipAddress.GetAddressBytes()[0];
                    numUDIP2.Value = ipAddress.GetAddressBytes()[1];
                    numUDIP3.Value = ipAddress.GetAddressBytes()[2];
                    numUDIP4.Value = ipAddress.GetAddressBytes()[3];

                    numRetries.Value = xmlreader.GetValueAsInt("MyMovies", "wakeupRetries", 3);
                    numRetryTimeout.Value = xmlreader.GetValueAsInt("MyMovies", "wakeupRetryTimeout", 3000);

                    numMaxRating.Value = xmlreader.GetValueAsInt("MyMovies", "maximumViewableRating", 4);

                }
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::Init - Cannot load settings");
                Log.Error(ex);
            }
        }

        internal void LoadGrid(DriveReplacements pathReplacements)
        {
            
            gridDriveReplacements.Rows.Clear();
            XmlNodeList nodes = pathReplacements.Select("//Path");

            foreach (XmlNode path in nodes)
            {
                string pathName = path.Attributes["name"].Value;
                gridDriveReplacements.Rows.Add(new string[] { pathName, path.InnerText });
            }
        }

        /// <summary>
        /// Load the configured users into the grid. 
        /// The Row index (ordering) identifies the user within the DB.
        /// </summary>
        /// <param name="xmlUsers">string from config file</param>
        internal void LoadUsers(string xmlUsers)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlUsers);
                XmlNodeList usersNodes = doc.DocumentElement.SelectNodes("//Users");
                foreach (XmlNode user in usersNodes)
                {
                    string userName = user.Attributes["name"].Value;
                    gridUsers.Rows.Add(new string[] { userName });
                }
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::LoadUsers - cannot parse '{0}' as xml", xmlUsers);
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Accessor method for the configured IP Address.
        /// </summary>
        /// <returns>IPAddress</returns>
        public IPAddress GetIPAddress()
        {
            return IPAddress.Parse(numUDIP1.Value.ToString() + '.' + numUDIP2.Value.ToString() + '.' + numUDIP3.Value.ToString() + '.' + numUDIP4.Value.ToString());
        }


        /// <summary>
        /// Accessor method for the configured MAC Address.
        /// </summary>
        /// <returns>MacAddress</returns>
        public MacAddress GetMacAddress()
        {
            byte[] bytes = new byte[6];
            bytes[0] = (byte)numUDMac1.Value;
            bytes[1] = (byte)numUDMac2.Value;
            bytes[2] = (byte)numUDMac3.Value;
            bytes[3] = (byte)numUDMac4.Value;
            bytes[4] = (byte)numUDMac5.Value;
            bytes[5] = (byte)numUDMac6.Value;

            return new MacAddress(bytes);
        }

        /// <summary>
        /// Save the current configuration and close the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    xmlwriter.SetValueAsBool("MyMovies", "chkRemoteWakeup", chkEnableRemoteWakeup.Checked);
                    xmlwriter.SetValue("MyMovies", "IPAddress",             GetIPAddress());
                    xmlwriter.SetValue("MyMovies", "MACAddress",            GetMacAddress());
                    xmlwriter.SetValue("MyMovies", "txtProgramDataPath",    txtProgramDataPath.Text);
                    xmlwriter.SetValue("MyMovies", "txtServerName",         txtServerName.Text);
                    xmlwriter.SetValue("MyMovies", "txtDBInstance",         txtDBInstance.Text);
                    xmlwriter.SetValue("MyMovies", "txtUserName",           txtUserName.Text);
                    xmlwriter.SetValue("MyMovies", "txtPassword",           txtPassword.Text);
                    xmlwriter.SetValue("MyMovies", "txtPINCode",            txtPINCode.Text);
                    xmlwriter.SetValue("MyMovies", "wakeupRetries",         numRetries.Value);
                    xmlwriter.SetValue("MyMovies", "wakeupRetryTimeout",    numRetryTimeout.Value);
                    xmlwriter.SetValue("MyMovies", "maximumViewableRating", numMaxRating.Value);
                    SaveDriveReplacement(xmlwriter);
                    SaveUsers(xmlwriter);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Coniguration::btnOK_Click - Error saving the configuration");
                Log.Error(ex);
            }
            DialogResult = DialogResult.OK;

            this.Close();
        }

        // Save all configured drive replacements.
        private void SaveDriveReplacement(MediaPortal.Profile.Settings xmlwriter)
        {
            StringBuilder xmlDoc = new StringBuilder();

            xmlDoc.Append("<PathReplacement>");
            foreach (DataGridViewRow row in gridDriveReplacements.Rows)
            {
                if (row.Cells[0].Value != null)
                {
                    xmlDoc.AppendFormat("\n<Path name=\"{0}\">{1}</Path>", row.Cells[0].Value.ToString(), row.Cells[1].Value.ToString());
                }
            }
            xmlDoc.Append("</PathReplacement>");

            xmlwriter.SetValue("MyMovies", "xmlPathReplacement", xmlDoc.ToString());
        }

        // Save all configured drive replacements.
        private void SaveUsers(MediaPortal.Profile.Settings xmlwriter)
        {
            StringBuilder xmlDoc = new StringBuilder();

            xmlDoc.Append("<Users>");
            foreach (DataGridViewRow row in gridUsers.Rows)
            {
                if (row.Cells[0].Value != null)
                {
                    xmlDoc.AppendFormat("\n<User name=\"{0}\"/>", row.Cells[0].Value.ToString());
                }
            }
            xmlDoc.Append("</Users>");

            xmlwriter.SetValue("MyMovies", "xmlUsers", xmlDoc.ToString());
        }
       
        /// <summary>
        /// Close the window without saving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void chkEnableRemoteWakeup_CheckedChanged(object sender, EventArgs e)
        {
            numUDMac1.Enabled = chkEnableRemoteWakeup.Checked;
            numUDMac2.Enabled = chkEnableRemoteWakeup.Checked;
            numUDMac3.Enabled = chkEnableRemoteWakeup.Checked;
            numUDMac4.Enabled = chkEnableRemoteWakeup.Checked;
            numUDMac5.Enabled = chkEnableRemoteWakeup.Checked;
            numUDMac6.Enabled = chkEnableRemoteWakeup.Checked;
            numUDIP1.Enabled  = chkEnableRemoteWakeup.Checked;
            numUDIP2.Enabled  = chkEnableRemoteWakeup.Checked;
            numUDIP3.Enabled  = chkEnableRemoteWakeup.Checked;
            numUDIP4.Enabled  = chkEnableRemoteWakeup.Checked;
            numRetries.Enabled = chkEnableRemoteWakeup.Checked;
            numRetryTimeout.Enabled = chkEnableRemoteWakeup.Checked;
        }

        private void tabControl_Selected(object sender, TabControlEventArgs e)
        {
            btnTestConnection.Visible = e.TabPage.Name.Equals("tabSqlConnection");
        }

        #region SQL Queries

        private const string _tblResumeExists = @"SELECT COUNT(*) FROM sysobjects WHERE id = OBJECT_ID('dbo.tblResume') AND OBJECTPROPERTY(id, N'IsUserTable') = 1";
        private const string _createTableResume = @"CREATE TABLE tblResume( intId INT NOT NULL, intUserId INT NOT NULL DEFAULT 0, intStopTime INT NOT NULL, nvcResumeData NVARCHAR(MAX) NOT NULL, nvcFileName NVARCHAR(255) NOT NULL DEFAULT(''), datLastPlayed DATETIME NOT NULL CONSTRAINT PK_tblResume PRIMARY KEY (intId, intUserId) )";
        private const string _userIdExists = @"SELECT COUNT(*) FROM sys.columns WHERE Name = 'intUserId' AND Object_ID = Object_ID(N'tblResume')";
        private const string _addUserId = @"ALTER TABLE tblResume DROP CONSTRAINT PK_tblResume; ALTER TABLE tblResume ADD intUserId	INT	NOT NULL DEFAULT 0; ALTER TABLE tblResume ADD CONSTRAINT PK_tblResume PRIMARY KEY(intId, intUserId)";

        #endregion

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                string connection = QueryReader<int>.Connection(txtServerName.Text, txtDBInstance.Text, txtUserName.Text, txtPassword.Text);
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 5;
                    cmd.CommandText = _tblResumeExists;

                    conn.Open();

                    // if the table does not exist, then create it.
                    if (System.Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        QueryUpdate createTable = new QueryUpdate(_createTableResume);
                        createTable.Execute(connection);
                    }
                    // else check if the UserId column exists
                    else
                    {
                        cmd.CommandText = _userIdExists;

                        // if the table does not exist, then create it.
                        if (System.Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            QueryUpdate createTable = new QueryUpdate(_addUserId);
                            createTable.Execute(connection);
                        }
                    }
                }

                TestConnectionResult(true);
            }
            catch (SqlException sqlEx)
            {
                TestConnectionResult(false);
                Log.Debug("MyMovies::btnTestConnection_Click - Sql Exception '{0}'", sqlEx.Message);
            }
            catch (Exception ex)
            {
                TestConnectionResult(false);
                Log.Debug("MyMovies::btnTestConnection_Click - Exception '{0}'", ex.Message);
            }
        }

        private void TestConnectionResult(bool success)
        {
            txtServerName.BackColor = (success) ? Color.LightGreen : Color.Red;
            txtDBInstance.BackColor = (success) ? Color.LightGreen : Color.Red;
            txtUserName.BackColor   = (success) ? Color.LightGreen : Color.Red;
            txtPassword.BackColor   = (success) ? Color.LightGreen : Color.Red;
        }

        private void txtReplacement_TextChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = (txtReplacement.Text.Length > 0) && (txtPath.Text.Length > 0);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            gridDriveReplacements.Rows.Add(new string[] { txtPath.Text, txtReplacement.Text });
            txtReplacement.Text = string.Empty;
            txtPath.Text = string.Empty;
        }

        private void numUD_Enter(object sender, EventArgs e)
        {
            NumericUpDown control = sender as NumericUpDown;
            if (control != null)
            {
                control.Select(0, 3);
            }
        }

        bool NumberEntered = false;

        //Check if key entered is "numeric".
        private bool CheckIfNumericKey(Keys K, bool isDecimalPoint)
        {
            if (K == Keys.Back) //backspace?
                return true;
            else if (K == Keys.OemPeriod || K == Keys.Decimal)  //decimal point?
                return isDecimalPoint ? false : true;       //or: return !isDecimalPoint
            else if ((K >= Keys.D0) && (K <= Keys.D9))      //digit from top of keyboard?
                return true;
            else if ((K >= Keys.NumPad0) && (K <= Keys.NumPad9))    //digit from keypad?
                return true;
            else
                return false;   //no "numeric" key
        }

        private void txtPINCode_KeyDown(object sender, KeyEventArgs e)
        {
            //Get our textbox.
            TextBox Tbx = (TextBox)sender;
            // Initialize the flag.
            NumberEntered = CheckIfNumericKey(e.KeyCode, Tbx.Text.Contains("."));
        }

        // This event occurs after the KeyDown event and can be used to prevent
        // characters from entering the control.
        private void txtPINCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check for the flag being set in the KeyDown event.
            if (NumberEntered == false)
            {
                // Stop the character from being entered into the control since it is non-numerical.
                e.Handled = true;
            }

        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            gridUsers.Rows.Add(new string[] { txtUser.Text });
            txtUser.Text = string.Empty;
        }

        private void txtUser_TextChanged(object sender, EventArgs e)
        {
            btnAddUser.Enabled = (txtUser.Text.Length > 0);
        }
    }
}