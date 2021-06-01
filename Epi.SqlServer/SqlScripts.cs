using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace Epi.SqlServer
{
    public class SqlScripts
    {
        public class SqlScriptResult
        {
            public bool Success { get; set; }
            public string  Message { get; set; }
        }

        public string ConnectionString { get; set; }



        private SqlScriptResult CreatePhysicalDatabase(string dbName)
        {
            try
            {
                SqlConnectionStringBuilder masterBuilder = new SqlConnectionStringBuilder(this.ConnectionString);
                masterBuilder.InitialCatalog = "Master";
                //masterBuilder.IntegratedSecurity = true;
                SqlConnection masterConnection = new SqlConnection(masterBuilder.ToString());
                var command = masterConnection.CreateCommand();
                command.CommandText = string.Format("SELECT Count(*) FROM sysdatabases WHERE name='{0}'", dbName);
                masterConnection.Open();
                object result = command.ExecuteScalar();
                if ((int)result == 0)
                {
                    command.CommandText = "create database [" + dbName + "]";
                    command.ExecuteNonQuery();
                }
                masterConnection.Close();

            }
            catch (SqlException ex)
            {
                return new SqlScriptResult { Success = false, Message = string.Format("Could not create new SQL database.\n{0}", ex.Message) };
            }
            finally
            {
            }

            return new SqlScriptResult
            {
                Success = true
            };
         }
             




        public SqlScriptResult RunScript(string dbName, string filePath)
        {
            string txtSQL = null;
            try
            {
                using (StreamReader sr = File.OpenText(filePath))
                {
                    //Read the entire file
                    sr.BaseStream.Seek(0, SeekOrigin.Begin);
                    txtSQL = sr.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                return new SqlScriptResult { Success=false, Message = string.Format("Could not read SQL script file.\n{0}", ex.Message) };
            }

            var dbResult =  CreatePhysicalDatabase(dbName);
            if (!dbResult.Success)
            {
                return new SqlScriptResult { Success = false, Message = dbResult.Message };
            }

            string[] SqlLine;
            Regex regex = new Regex("^GO(\r\n|\n|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            SqlLine = regex.Split(txtSQL);

            var result = new SqlScriptResult();

            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand{Connection = conn})
                    {
                        foreach (string line in SqlLine)
                        {
                            if (line.Length > 0)
                            {                              
                                cmd.CommandText = line;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        
                    }                     
                    conn.Close();
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = string.Format("Could not run SQL Script.\n{0}", ex.Message);
                }

            }

            return result;
        }

    }
}
