using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using RestSharp;


namespace ConsoleTest
{
    class Program
    {
        private static DateTimeOffset _lastQuery;

        static void Main(string[] args)
        {
            _lastQuery = DateTimeOffset.UtcNow;
            CloudTable table = GetLogsTable();

            while (true)
            {
                string message = GetLogs(table);
                if (string.IsNullOrWhiteSpace(message))
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    continue;
                }
                StreamToUrl(message);
            }
        }

        private static CloudTable GetLogsTable()
        {
            string cs = ConfigurationManager.AppSettings["StorageConnectionString"];
            var storageAccount = CloudStorageAccount.Parse(cs);

            var tableClient = storageAccount.CreateCloudTableClient();
            //"WADWindowsEventLogsTable"
            //"WADLogsTable"
            CloudTable table = tableClient.GetTableReference("WADLogsTable");
            return table;
        }

        private static string GetLogs(CloudTable table)
        {
            var query = new TableQuery<LogEntity>()
                .Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, _lastQuery));

            var sb = new StringBuilder();
            foreach (LogEntity entry in table.ExecuteQuery(query))
            {
                sb.Append(entry.Message);
                sb.Append(", ");
                sb.Append(entry.Timestamp);
                sb.AppendLine();
                _lastQuery = entry.Timestamp;
            }

            return sb.ToString();
        }

        //TODO: gzip https://service.sumologic.com/ui/help/Default.htm#cshid=4036
        static void StreamToUrl(string content)
        {
            var client = new RestClient();
            client.BaseUrl = "https://collectors.sumologic.com";
            var request = new RestRequest();
            request.Resource = "receiver/v1/http/ZaVnC4dhaV1aogV4rZCv2xBccsc1PBemC6tFssqEyk2bMVkLe1_4We12sWQdqALkwWdt8ncj7NaN1OSILQMhsBPupMs7mAAgIjWDprP4GQjlciCO_17Urw==";
            request.Method = Method.POST;
            request.AddFile("someName", Encoding.UTF8.GetBytes(content), "someFileName");

            IRestResponse response = client.Execute(request);
        }
    }
}
