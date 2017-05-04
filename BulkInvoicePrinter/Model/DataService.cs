using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace GOFetchBulkInvoicePrinter.Model
{
    public class DataService : IDataService
    {

        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(System.Configuration.ConfigurationManager.ConnectionStrings["GOFetchBulkInvoicePrinter.Properties.Settings.TaskCatAddress"].ConnectionString),
        };

        public DataService()
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public async void GetJob(string JOBID, Action<JObject, Exception> callback)
        {
            // Use this to connect to the actual data service

            try
            {
                HttpResponseMessage response = await client.GetAsync(string.Format("/api/job/{0}/", JOBID));
                response.EnsureSuccessStatusCode(); // Throw on error code.
                string x = await response.Content.ReadAsStringAsync();
                JObject jo = JObject.Parse(x);
                callback(jo, null);
            }
            catch (Exception ex)
            {
                callback(null, ex);
            }
        }
    }
}