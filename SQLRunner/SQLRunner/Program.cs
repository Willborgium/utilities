using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLRunner.Properties;

namespace SQLRunner
{
    public class Program
    {
        static string GetFileText(string filePath)
        {
            string output = null;

            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                {
                    output = reader.ReadToEnd();
                }
            }

            return output;
        }

        static void ExecuteSQL(SqlConnection connection, string category, IEnumerable<string> executionOrder)
        {
            Console.WriteLine("Running {0}'s...", category);

            var createRoot = Path.Combine(Settings.Default.RootDirectory, category);

            var successCount = 0;

            foreach (var item in executionOrder)
            {
                try
                {
                    var commandText = GetFileText(Path.Combine(createRoot, string.Format("{0}.dbo.{1}.sql", category, item)));

                    if (!string.IsNullOrWhiteSpace(commandText))
                    {
                        var command = new SqlCommand(commandText, connection);

                        command.ExecuteNonQuery();

                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine("No {0} command found for '{1}'.", category, item);
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to {0} '{1}'.", category, item);
                }
            }

            Console.WriteLine("{0}'s complete. {1} of {2} succeeded...", category, successCount, executionOrder.Count());
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Reading execution file...");

            var executionString = GetFileText(Path.Combine(Settings.Default.RootDirectory, Settings.Default.ExecutionFile));

            if (!string.IsNullOrWhiteSpace(executionString))
            {
                var executionOrder = executionString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine(string.Format("Found {0} steps...", executionOrder.Length));

                using (var connection = new SqlConnection(Settings.Default.ConnectionString))
                {
                    connection.Open();

                    if (Settings.Default.DoCreate)
                    {
                        ExecuteSQL(connection, "CREATE", executionOrder);
                    }

                    if (Settings.Default.DoAlter)
                    {
                        ExecuteSQL(connection, "ALTER", executionOrder);
                    }

                    if (Settings.Default.DoInsert)
                    {
                        ExecuteSQL(connection, "INSERT", executionOrder);
                    }
                }

                Console.WriteLine("Complete!");
            }
            else
            {
                Console.WriteLine("Failed to load execution file, or it is empty...");
            }

            Console.ReadKey();
        }
    }
}