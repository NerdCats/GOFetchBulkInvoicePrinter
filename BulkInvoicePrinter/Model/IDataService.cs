using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace GOFetchBulkInvoicePrinter.Model
{
    public interface IDataService
    {
        void GetJob(string JOBID, Action<JObject, Exception> callback);
    }
}
