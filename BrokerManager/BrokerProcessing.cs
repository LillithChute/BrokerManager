using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using CsvHelper;
using BrokerManager.Models;
using BrokerManager.Services;
using System.Collections.Generic;

namespace BrokerManager
{
    class BrokerProcessing
    {
        public static IConfigurationRoot Configuration;

        static void Main(string[] args)
        {
            ServiceProvider serviceProvider = GetServiceConfiguration();
            List<Broker> processedBrokers = new List<Broker>();

            System.Console.WriteLine("Begin proccess...");

            // Get the data file.  In a real scenario this would be fed in via file upload or as a batch load etc.
            TextReader fileReader;
            StreamWriter fileWriter;
            try
            {
                fileReader = File.OpenText("C:\\Resources\\DataToUpload.csv");
                fileWriter = File.CreateText("C:\\Resources\\GroupedBrokersNew.csv");
            }
            catch (System.Exception)
            {
                throw new System.Exception("Unable to set up filereader/filewriter.");
            }

            // Don't set up mapping for now.  Would set up multiple mappings depending on company if this were real.
            var csvRead = new CsvReader(fileReader);
            var csvWrite = new CsvWriter(fileWriter);

            // Log bad data.
            csvRead.Configuration.BadDataFound = context =>
            {
                serviceProvider.GetService<Logger>().errorMessage($"Bad data found on row '{context.RawRow}'");
            };

            // Ignore the Group ID field since that's what we're making.  We'll set that later
            csvRead.Configuration.MissingFieldFound = null;
            csvRead.Configuration.HeaderValidated = null;

            var brokerRecords = new List<Broker>();
            try
            {
                // Read in the data
                System.Console.WriteLine("Reading data file...");
                brokerRecords = csvRead.GetRecords<Broker>().ToList();

                // Group
                System.Console.WriteLine("Grouping brokers...");
                List<List<Broker>> groups = GroupBrokers(brokerRecords);

                // Assign group ID
                System.Console.WriteLine("Assigning group ID...");
                AssignGroupId(processedBrokers, groups);
            }
            catch (System.Exception e)
            {
                throw new System.Exception("Unable to read data records: " + e.Message);
            }

            System.Console.WriteLine("Writing data file...");

            csvWrite.WriteRecords(processedBrokers);

            System.Console.WriteLine("Process complete.");
        }

        #region Proccess Broker Data
        private static void AssignGroupId(List<Broker> processedBrokers, List<List<Broker>> groups)
        {
            int count = 0;
            foreach (var records in groups)
            {
                foreach (var record in records)
                {
                    try
                    {
                        record.GROUP_ID = count.ToString();

                        // Group ID is assigned add the broker to the final list
                        processedBrokers.Add(record);
                    }
                    catch (System.Exception)
                    {
                        throw new System.Exception("Group ID assignment failed.");
                    }
                }

                count++;
            }
        }

        private static List<List<Broker>> GroupBrokers(List<Broker> brokerRecords)
        {
            // We have to assume that we need to group not only on US Addresses but foreign as well so add those in.
            return brokerRecords.GroupBy(c => new
            {
                c.INS_BROKER_NAME,
                c.INS_BROKER_US_ADDRESS1,
                c.INS_BROKER_US_ADDRESS2,
                c.INS_BROKER_US_CITY,
                c.INS_BROKER_US_STATE,
                c.INS_BROKER_US_ZIP,
                c.INS_BROKER_FOREIGN_ADDRESS1,
                c.INS_BROKER_FOREIGN_ADDRESS2,
                c.INS_BROKER_FOREIGN_CITY,
                c.INS_BROKER_FOREIGN_PROV_STATE,
                c.INS_BROKER_FOREIGN_POSTAL_CD,
                c.INS_BROKER_FOREIGN_CNTRY
            })
                    .Select(group => group.ToList())
                    .ToList();
        }
        #endregion

        #region Configuration
        private static ServiceProvider GetServiceConfiguration()
        {
            // create service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Add logging service
            serviceCollection.AddSingleton(new LoggerFactory().AddConsole().AddDebug());
            serviceCollection.AddLogging();

            // build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // If we wanted to bulk copy this to a SQL Server table this is where we would get the connection info
            Configuration = builder.Build();
            string connectionstring = Configuration["ConnectionString"];

            serviceCollection.AddOptions();
            //serviceCollection.Configure<LogSettings>(configuration.GetSection("Configuration"));
            //ConfigureConsole(configuration);

            // add services
            serviceCollection.AddTransient<Interfaces.ILogging, LoggingService>();

            // add logger service to service collection
            serviceCollection.AddTransient<Logger>();
        }
        #endregion
    }
}
