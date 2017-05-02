using GalaSoft.MvvmLight;
using GOFetchBulkInvoicePrinter.Model;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GOFetchBulkInvoicePrinter.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        public const string WelcomeTitlePropertyName = "WelcomeTitle";

        private string _welcomeTitle = string.Empty;

        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }
            set
            {
                Set(ref _welcomeTitle, value);
            }
        }

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    WelcomeTitle = item.Title;
                });
        }   

        #region Properties

        private string _JobIDs;

        public string JobIDs
        {
            get { return _JobIDs; }
            set
            {
                _JobIDs = value;
                base.RaisePropertyChanged("JobIDs");
            }
        }

        public List<string> JobIDList { get; set; } = new List<string>();

        public List<Job> FetchedJobs { get; set; } = new List<Job>();

        #endregion

        private RelayCommand _PrintInvoices;

        public RelayCommand PrintInvoices
        {
            get
            {
                return _PrintInvoices
                    ?? (_PrintInvoices = new RelayCommand(
                    () =>
                    {
                        if (!string.IsNullOrEmpty(JobIDs))
                        {
                            JobIDList = new List<string>();
                            JobIDList = JobIDs.Split(' ').ToList();

                            FetchedJobs = new List<Job>();

                            foreach (var JobID in JobIDList)
                            {
                                _dataService.GetJob(JobID, (res, err) =>
                                                   {
                                                       if (err != null)
                                                       {
                                                           MessageBox.Show(err.Message, "Error!"); return;
                                                       }

                                                       if (res != null)
                                                       {
                                                           try
                                                           {
                                                               Job job = new Job()
                                                               {
                                                                   TrackingNo = (string)res.SelectToken("HRID"),
                                                                   ReferenceInvoiceId = (string)res.SelectToken("Order.ReferenceInvoiceId"),
                                                                   Date = DateTime.Today.ToLongDateString(),
                                                                   PaymentMethod = (string)res.SelectToken("PaymentMethod"),
                                                                   Assets = JObject.Parse(res["Assets"].ToString()),
                                                                   Tasks = res.SelectToken("Tasks").ToArray(),
                                                               };

                                                               string tasks1, tasks2, tasks3 = string.Empty;

                                                               tasks1 = (string)job.Tasks[1].SelectToken("AssetRef");
                                                               tasks2 = (string)job.Tasks[2].SelectToken("AssetRef");

                                                               if (job.Tasks.Count() > 3)
                                                               {
                                                                   tasks3 = (string)job.Tasks[3].SelectToken("AssetRef");
                                                               }

                                                               Dictionary<string, string> assetnames = new Dictionary<string, string>();
                                                               foreach (var item in job.Assets)
                                                               {
                                                                   assetnames.Add((string)item.Key, (string)item.Value.SelectToken("UserName"));
                                                               }

                                                               job.PickupBy = assetnames.FirstOrDefault(x => x.Key == tasks1).Value;
                                                               job.DeliveryBy = assetnames.FirstOrDefault(x => x.Key == tasks2).Value;
                                                               job.CashDeliveryBy = job.Tasks.Count() > 3 ? assetnames.FirstOrDefault(x => x.Key == tasks3).Value : "";

                                                               job.PickupTime = (string)job.Tasks[1].SelectToken("ETA");
                                                               job.PickupAddress = (string)res.SelectToken("Order.From.AddressLine1");

                                                               job.ServiceCharge = (string)res.SelectToken("Order.OrderCart.ServiceCharge");
                                                               job.TotalToPay = (string)res.SelectToken("Order.OrderCart.TotalToPay");
                                                               job.SpecialNotetoDeliveryMan = (string)res.SelectToken("Order.NoteToDeliveryMan");
                                                               job.Order = JObject.Parse(res["Order"].ToString());
                                                               job.OrderCart = JObject.Parse(job.Order["OrderCart"].ToString());
                                                               job.PackageList = job.OrderCart.SelectToken("PackageList").ToArray();

                                                               job.DeliveryTime = (string)job.Tasks[2].SelectToken("ETA");
                                                               job.DeliveryAddress = (string)res.SelectToken("Order.To.AddressLine1");
                                                               job.PackageList = job.OrderCart.SelectToken("PackageList").ToArray();

                                                               FetchedJobs.Add(job);

                                                           }
                                                           catch (System.Exception ex)
                                                           {
                                                               MessageBox.Show(ex.Message, "Error parsing job!"); return;
                                                           }
                                                       }

                                                   });
                            }
                        }

                    }));
            }
        }
    }
}