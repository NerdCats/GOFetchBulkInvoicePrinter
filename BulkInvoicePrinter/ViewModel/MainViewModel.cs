using GalaSoft.MvvmLight;
using GOFetchBulkInvoicePrinter.Model;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Reporting.WinForms;
using BulkInvoicePrinter;

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

        private PrinterClass _printerClass;

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

        public const string WelcomeTitlePropertyName = "WelcomeTitle";

        private event EventHandler InvoiceGet_Completed;

        List<ReportParameter> reportParameters = new List<ReportParameter>();
        ReportParameter prCredentials = new ReportParameter();

        #region Properties

        #region WelcomeTitle

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

        #endregion

        #region JobIDs

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

        #endregion

        #region JobIndex

        private int _JobIndex = 0;

        public int JobIndex
        {
            get { return _JobIndex; }
            set { _JobIndex = value; base.RaisePropertyChanged("JobIndex"); }
        }
        #endregion

        public List<string> JobIDList { get; set; } = new List<string>();

        public List<Job> FetchedJobs { get; set; } = new List<Job>();

        #endregion

        #region Relay Commands

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
                            JobIDList = (JobIDs.Contains(",")) ? JobIDs.Split(',').ToList() : JobIDs.Split(' ').ToList();
                            FetchedJobs = new List<Job>();

                            if (JobIDList.Count > 0)
                            {
                                JobIndex = 0;
                                InvoiceGet_Completed += MainViewModel_InvoiceGet_Completed;
                                GetInvoice(JobIDList[JobIndex]);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Nothing to print. No Job IDs were pasted.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                    }));
            }
        }

        #endregion

        #region Private Methods

        private void MainViewModel_InvoiceGet_Completed(object sender, EventArgs e)
        {
            if (JobIndex < JobIDList.Count)
            {
                GetInvoice(JobIDList[JobIndex]);
            }
        }

        private void GetInvoice(string JobID)
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

                        foreach (var citem in job.PackageList)
                        {
                            PackageItem pi = new PackageItem()
                            {
                                Item = (string)citem.SelectToken("Item"),
                                Quantity = (decimal)citem.SelectToken("Quantity"),
                                Price = (decimal)citem.SelectToken("Price"),
                                Weight = (decimal)citem.SelectToken("Weight"),
                                Total = (decimal)citem.SelectToken("Total"),
                            };

                            job.PackageItemList.Add(pi);
                        }

                        FetchedJobs.Add(job);

                        JobIndex++;

                        SendToPrinter(job);

                        if (InvoiceGet_Completed != null)
                        {
                            MainViewModel_InvoiceGet_Completed(true, null);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error parsing job!", MessageBoxButton.OK, MessageBoxImage.Error); return;
                    }
                }

            });
        }

        private void SendToPrinter(Job job)
        {
            reportParameters = new List<ReportParameter>();

            prCredentials = new ReportParameter("TrackingNo", job.TrackingNo);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("ReferenceInvoiceId", job.ReferenceInvoiceId);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("Date", job.Date);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("PaymentMethod", job.PaymentMethod);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("PickupBy", job.PickupBy);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("DeliveryBy", job.DeliveryBy);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("CashDeliveryBy", job.CashDeliveryBy);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("PickupTime", job.PickupTime);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("PickupAddress", job.PickupAddress);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("DeliveryTime", job.DeliveryTime);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("DeliveryAddress", job.DeliveryAddress);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("ServiceCharge", job.ServiceCharge);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("TotalToPay", job.TotalToPay);
            reportParameters.Add(prCredentials);

            prCredentials = new ReportParameter("SpecialNotetoDeliveryMan", job.SpecialNotetoDeliveryMan);
            reportParameters.Add(prCredentials);

            DataSet.PackageListDataTable pldt = new DataSet.PackageListDataTable();

            foreach (var citem in job.PackageItemList)
            {
                pldt.AddPackageListRow(citem.Item, citem.Quantity, citem.Price, citem.Weight, citem.Total);
            }

            _printerClass.Run(pldt, reportParameters);
        }

        #endregion
    }
}