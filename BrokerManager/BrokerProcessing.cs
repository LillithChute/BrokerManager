using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using CsvHelper;
using BrokerManager.Models;
using BrokerManager.Services;

namespace BrokerManager
{
    class BrokerProcessing
    {
        public static IConfigurationRoot Configuration;

        static void Main(string[] args)
        {
            ServiceProvider serviceProvider = GetServiceConfiguration();

            // Get the data file.  In a real scenario this would be fed in via file upload or as a batch load etc.
            TextReader fileReader = File.OpenText("C:\\Resources\\DataToUpload.csv");
            StreamWriter fileWriter = File.CreateText("C:\\Resources\\GroupedBrokers.csv");

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

            System.Console.WriteLine("Reading data file...");

            // Read in the data
            var brokerRecords = csvRead.GetRecords<Broker>().ToList();

            // Group the records
            System.Console.WriteLine("Grouping brokers.");

            //  This is really, really slow but it will be effective as a quick and dirty.  I think this is an O(n^2).  Eew.
            GroupBrokers(brokerRecords);

            System.Console.WriteLine("Writing data file...");

            csvWrite.WriteRecords(brokerRecords);

            System.Console.WriteLine("Process complete.");
        }

        //  Do the actual grouping within the model.
        private static void GroupBrokers(System.Collections.Generic.List<Broker> brokerRecords)
        {
            try
            {
                var groupId = 0;
                foreach (var broker in brokerRecords)
                {
                    brokerRecords.Where(
                        w =>
                        w.INS_BROKER_NAME == broker.INS_BROKER_NAME &&
                        w.INS_BROKER_US_ADDRESS1 == broker.INS_BROKER_US_ADDRESS1 &&
                        w.INS_BROKER_US_ADDRESS2 == broker.INS_BROKER_US_ADDRESS2 &&
                        w.INS_BROKER_US_CITY == broker.INS_BROKER_US_CITY &&
                        w.INS_BROKER_US_STATE == broker.INS_BROKER_US_STATE &&
                        w.INS_BROKER_US_ZIP == broker.INS_BROKER_US_ZIP &&
                        w.INS_BROKER_FOREIGN_ADDRESS1 == broker.INS_BROKER_FOREIGN_ADDRESS1 &&
                        w.INS_BROKER_FOREIGN_ADDRESS2 == broker.INS_BROKER_FOREIGN_ADDRESS2 &&
                        w.INS_BROKER_FOREIGN_CITY == broker.INS_BROKER_FOREIGN_CITY &&
                        w.INS_BROKER_FOREIGN_CNTRY == broker.INS_BROKER_FOREIGN_CNTRY &&
                        w.INS_BROKER_FOREIGN_POSTAL_CD == broker.INS_BROKER_FOREIGN_POSTAL_CD &&
                        w.INS_BROKER_FOREIGN_PROV_STATE == broker.INS_BROKER_FOREIGN_PROV_STATE
                        )
                        .ToList()
                        .ForEach(f => f.GROUP_ID = groupId.ToString());

                    groupId += 1;
                }
            }
            catch (System.Exception)
            {
                throw new System.Exception("Grouping failed.");
            }
        }

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
