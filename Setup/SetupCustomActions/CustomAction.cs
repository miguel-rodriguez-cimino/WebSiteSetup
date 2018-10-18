using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Web.Administration;
using System.Net;
using System.Security.Principal;
using Microsoft.Win32;

namespace SetupCustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult LoadWebSites(Session session)
        {
            if(!CanSetIis())
            {
                session["CANNOTSETIIS"] = "1";
                session["SKIPIIS"] = "1";
                return ActionResult.Success;
            }

            ServerManager serverManager = new ServerManager();
            View comboBoxTable = session.Database.OpenView("select * from ComboBox");
            comboBoxTable.Execute();
            if (comboBoxTable.Count() == 1 && comboBoxTable.First().GetString(3) == "@NEW")
            {
                int siteCount = 1;
                try
                {
                    foreach (Site site in serverManager.Sites)
                    {
                        StringBuilder sb = new StringBuilder(string.Format("{0} (", site.Name));

                        foreach (Binding binding in site.Bindings)
                        {
                            sb.Append(string.Format("{0}|", binding.ToString()));
                        }
                        sb.Length--;
                        sb.Append(")");

                        Record newRecord = session.Database.CreateRecord(4);
                        newRecord.SetString(1, "IISWEBSITE");
                        newRecord.SetInteger(2, ++siteCount);
                        newRecord.SetString(3, site.Name);
                        newRecord.SetString(4, sb.ToString());

                        comboBoxTable.Modify(ViewModifyMode.InsertTemporary, newRecord);
                    }
                }
                catch (Exception e)
                {
                    Record errorRecord = new Record();
                    errorRecord.FormatString = string.Format("Error executing custom action LoadWebSites():\r\n\t{0}\r\n\t{1}\r\n\t{2}", e.GetType().Name, e.Message, e.StackTrace);
                    session.Message(InstallMessage.Error, errorRecord);
                }
            }

            comboBoxTable.Close();
            session["IISWEBSITE"] = "@NEW";
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckDatabase(Session session)
        {
            bool connectionOK = false;
            string serverName = session["DBSERVERNAME"];
            string authMethod = session["AUTHMETHOD"];
            string userName = session["DBUSR"];
            string password = session["DBPWD"];
            string connectionString = string.Empty;
            serverName = serverName.Trim();
            userName = userName.Trim();
            password = password.Trim();

            if (authMethod == "1")
            {
                if (!string.IsNullOrEmpty(serverName))
                {
                    connectionString = string.Format("Data Source={0};Initial Catalog=master;Integrated Security=True", serverName);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(serverName) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                {
                    connectionString = string.Format("Data Source={0};Initial Catalog=master;User ID={1};Password={2}", serverName, userName, password);
                }
            }

            if (!string.IsNullOrEmpty(connectionString))
            {
                using (SqlConnection cnx = new SqlConnection(connectionString))
                {
                    try
                    {
                        cnx.Open();
                        connectionOK = true;
                    }
                    catch
                    {
                        connectionOK = false;
                    }
                }
            }

            session["DBCONNECTIONSUCCESS"] = connectionOK ? "1" : "0";
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckIis(Session session)
        {
            string errorMsg = string.Empty;
            string webSite = session["IISWEBSITE"];
            ServerManager serverManager = new ServerManager();

            if (webSite == "@NEW")
            {
                string siteName = session["SITENAME"].Trim();
                string siteIp = session["SITEIP"].Trim();
                string sitePort = session["SITEPORT"].Trim();
                int portNumber = string.IsNullOrEmpty(sitePort) ? 0 : Convert.ToInt32(sitePort);
                IPAddress ipAddress = null;

                string oldVersionSiteName = session["OLDSITENAME"].Trim();
                string oldIp = session["OLDIP"].Trim();
                string oldPort = session["OLDPORT"].Trim();

                if (string.IsNullOrEmpty(siteName) || Regex.IsMatch(siteName, "[\\\\/?;:@&=+$,|\"<>]"))
                {
                    errorMsg = session["IISSITENAMEINVALID"];
                }

                if (string.IsNullOrEmpty(errorMsg)
                    && !siteName.Equals(oldVersionSiteName, StringComparison.InvariantCultureIgnoreCase)
                    && serverManager.Sites.Any(x => x.Name == siteName))
                {
                    errorMsg = session["IISSITENAMEEXISTS"];
                }

                if (string.IsNullOrEmpty(errorMsg) && siteIp != "*")
                {
                    if (string.IsNullOrEmpty(siteIp) || siteIp.Count(x => x == '.') != 3 || !IPAddress.TryParse(siteIp, out ipAddress))
                    {
                        errorMsg = session["IISIPINVALID"];
                    }
                }

                if (string.IsNullOrEmpty(errorMsg) && (portNumber < 1 || portNumber > 65535))
                {
                    errorMsg = session["IISPORTINVALID"];
                }

                if (string.IsNullOrEmpty(errorMsg))
                {
                    if (!siteIp.Equals(oldIp, StringComparison.InvariantCultureIgnoreCase) || !sitePort.Equals(oldPort, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (siteIp == "*")
                        {
                            ipAddress = new IPAddress(new byte[] { 0, 0, 0, 0 });
                        }

                        if (serverManager.Sites.SelectMany(x => x.Bindings).Any(x => x.EndPoint != null && x.EndPoint.Address.Equals(ipAddress) && x.EndPoint.Port == portNumber))
                        {
                            errorMsg = session["IISBINDINGINUSE"];
                        }
                    }
                }
            }
            else
            {
                string virtualDirectoryAlias = session["VIRTUALDIRECTORY"];
                string oldDirectoryAlias = session["OLDVDIR"];

                Site site = serverManager.Sites.FirstOrDefault(x => x.Name == webSite);
                if (site != null && !virtualDirectoryAlias.Equals(oldDirectoryAlias, StringComparison.InvariantCultureIgnoreCase) && site.Applications.SelectMany(x => x.VirtualDirectories).Any(x => x.Path.EndsWith(string.Concat("/", virtualDirectoryAlias))))
                {
                    errorMsg = string.Concat(session["VIRTUALDIREXISTS"], virtualDirectoryAlias);
                }
                session["SITEID"] = site.Id.ToString();
            }

            session["IISERRORMSG"] = errorMsg;
            session["IISSUCCESS"] = string.IsNullOrEmpty(errorMsg) ? "1" : "0";

            return ActionResult.Success;
        }

        private static bool CanSetIis()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
            var hasElevatedPrivileges = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            var hasIisInstalled = false;

            if (hasElevatedPrivileges)
            {
                try
                {
                    using (RegistryKey iisKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp"))
                    {
                        hasIisInstalled = (int)iisKey.GetValue("MajorVersion") >= 6;
                    }
                }
                catch
                {
                    hasIisInstalled = false;
                }
            }

            return hasElevatedPrivileges && hasIisInstalled;
        }
    }
}
