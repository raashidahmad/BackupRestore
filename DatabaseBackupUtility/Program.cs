using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DatabaseBackupUtility
{
    class Program
    {
        /* Local SQL Server connecton string */
        static string connectionString = "Server=.; Database=AIMSDb; User ID=sa;Password=master002; MultipleActiveResultSets=true";

        //Optional:connect using credentials
        //static string connectionString = "Server=localhost;user id=user2018;password=MYDBPASSWORD;";

        /* Database names to backup */
        static string[] saDatabases = new string[] { "AIMSDb" };

        /* Backup directory. Please note: Files older than DeletionDays old will be deleted automatically */
        static string backupDir = @"E:\DB_Backups";
        static string sqlBackupFile = @"E:\DB_Backups\AIMSDb_2019-11-06_11-02-06-PM.bak";

        /* Delete backups older than DeletionDays. Set this to 0 to never delete backups */
        static int DeletionDays = 30;
        static Mutex mutex = new Mutex(true, "Global\\SimpleDBBackupMutex");

        static void Main(string[] args)
        {
            // allow only single instance of the app
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("Program already running!");
                return;
            }

            try
            {
                RestoreDatabase(sqlBackupFile);
                Console.ReadLine();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            mutex.ReleaseMutex();

            /*if (DeletionDays > 0)
                DeleteOldBackups();

            DateTime dtNow = DateTime.Now;

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();

                    foreach (string dbName in saDatabases)
                    {
                        string backupFileNameWithoutExt = String.Format("{0}\\{1}_{2:yyyy-MM-dd_hh-mm-ss-tt}", backupDir, dbName, dtNow);
                        string backupFileNameWithExt = String.Format("{0}.bak", backupFileNameWithoutExt);
                        string zipFileName = String.Format("{0}.zip", backupFileNameWithoutExt);

                        string cmdText = string.Format("BACKUP DATABASE {0}\r\nTO DISK = '{1}'", dbName, backupFileNameWithExt);

                        using (SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection))
                        {
                            sqlCommand.CommandTimeout = 0;
                            sqlCommand.ExecuteNonQuery();
                        }

                        using (ZipFile zip = new ZipFile())
                        {
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zip.AddFile(backupFileNameWithExt);
                            zip.Save(zipFileName);
                        }

                        //File.Delete(backupFileNameWithExt);
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }*/
        }

        static void RestoreDatabase(string backupFile)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(connectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        sqlConnection.Open();
                        sqlCommand.Connection = sqlConnection;
                        string cmdText = "select physical_name from sys.database_files where type = 0";
                        sqlCommand.CommandText = cmdText;
                        string fullPath = sqlCommand.ExecuteScalar().ToString();
                        string directoryPath = Path.GetDirectoryName(fullPath);
                        string fileName = Path.GetFileName(fullPath);
                        string fileExtension = Path.GetExtension(fileName);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        string dataFilePath = directoryPath + "//" + fileNameWithoutExtension + ".mdf";
                        string logFilePath = directoryPath + "//" + fileNameWithoutExtension + "_log.ldf";

                        sqlConnection.ChangeDatabase("master");
                        cmdText = "ALTER DATABASE[AIMSDb] SET Single_User WITH Rollback Immediate";
                        sqlCommand.CommandText = cmdText;
                        sqlCommand.ExecuteNonQuery();

                        cmdText = "RESTORE DATABASE AIMSDb FROM DISK = '" + backupFile + "'" +
                                    "WITH REPLACE";
                        sqlCommand.CommandText = cmdText;
                        sqlCommand.ExecuteNonQuery();

                        cmdText = "RESTORE DATABASE AIMSDb FROM DISK = '" + backupFile + "'" +
                                    "WITH REPLACE";
                        sqlCommand.CommandText = cmdText;
                        sqlCommand.ExecuteNonQuery();

                        cmdText = "ALTER DATABASE[AIMSDb] SET Multi_User";
                        sqlCommand.CommandText = cmdText;
                        sqlCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void DeleteOldBackups()
        {
            try
            {
                string[] files = Directory.GetFiles(backupDir);

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.CreationTime < DateTime.Now.AddDays(-DeletionDays))
                        fi.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
