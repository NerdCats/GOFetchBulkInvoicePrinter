﻿using GalaSoft.MvvmLight;
using GOFetchBulkInvoicePrinter.Model;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Reporting.WinForms;
using GoFetchBulkInvoicePrinter;

namespace GOFetchBulkInvoicePrinter.ViewModel
{    
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private event EventHandler InvoiceGet_Completed;
        private PrinterClass _printerClass = new PrinterClass();

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            WelcomeTitle = "Paste Job IDs to start bulk printing.";
            this.PrintButtonIsEnabled = true;
        }

        public const string WelcomeTitlePropertyName = "WelcomeTitle";

        List<ReportParameter> reportParameters = new List<ReportParameter>();
        ReportParameter prCredentials = new ReportParameter();

        #region Properties

        #region PrintingStatus

        private string _PrintingStatus = "Idle";

        public string PrintingStatus
        {
            get { return _PrintingStatus; }
            set { _PrintingStatus = value; base.RaisePropertyChanged("PrintingStatus"); }
        }
        
        #endregion

        #region PrintButtonIsEnabled

        private bool _PrintButtonIsEnabled;

        public bool PrintButtonIsEnabled
        {
            get { return _PrintButtonIsEnabled; }
            set
            {
                _PrintButtonIsEnabled = value;
                base.RaisePropertyChanged("PrintButtonIsEnabled");
            }
        }


        #endregion

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
                        try
                        {
                            JobIndex = 0;

                            if (!string.IsNullOrEmpty(JobIDs))
                            {
                                this.PrintButtonIsEnabled = false;

                                JobIDs = JobIDs.Trim();
                                JobIDList = new List<string>();
                                JobIDList = (JobIDs.Contains(",")) ? JobIDs.Split(',').ToList() : JobIDs.Split(' ').ToList();
                                if (JobIDList[JobIndex].Length > 12)
                                {
                                    MessageBox.Show("Sorry! Looks like the text you pasted is not a colection of Job IDs. Try again.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning); this.PrintButtonIsEnabled = true; return;
                                }
                                if (!JobIDList[JobIndex].StartsWith("Job"))
                                {
                                    MessageBox.Show("Sorry! Looks like the text you pasted is not a colection of Job IDs. Try again.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning); this.PrintButtonIsEnabled = true; return;
                                }

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
                                MessageBox.Show("Nothing to print. No Job IDs were pasted.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning); this.PrintButtonIsEnabled = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error); this.PrintButtonIsEnabled = true; return;
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
            else
            {
                _printerClass.Dispose();
                this.PrintButtonIsEnabled = true;
            }
        }

        private void GetInvoice(string JobID)
        {
            try
            {
                if (!string.IsNullOrEmpty(JobID))
                {
                    _dataService.GetJob(JobID, (res, err) =>
                           {
                               if (err != null)
                               {
                                   MessageBox.Show(err.Message, err.Message); this.PrintButtonIsEnabled = true;

                                   JobIndex++;
                                   PropagateToNextJob();

                                   return;
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

                                       this.PrintingStatus = "Printing";
                                       SendToPrinter(job);
                                       this.PrintingStatus = "Idle";
                                       PropagateToNextJob();
                                   }
                                   catch (System.Exception ex)
                                   {
                                       MessageBox.Show(ex.InnerException.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);                                       

                                       JobIndex++;
                                       PropagateToNextJob();
                                       return;
                                   }
                               }
                               else
                               {
                                   MessageBox.Show(string.Format("{0} Job not found. Press 'OK' to go to next job.", JobID), "", MessageBoxButton.OK, MessageBoxImage.Error);

                                   JobIndex++;
                                   PropagateToNextJob();
                               }
                           });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error); this.PrintButtonIsEnabled = true; return;
            }
        }

        private void PropagateToNextJob()
        {
            if (InvoiceGet_Completed != null)
            {
                MainViewModel_InvoiceGet_Completed(true, null);
            }
        }

        private void SendToPrinter(Job job)
        {
            try
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

                _printerClass.Run(pldt, reportParameters, "GoFetchBulkInvoicePrinter.Invoice.rdlc");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}