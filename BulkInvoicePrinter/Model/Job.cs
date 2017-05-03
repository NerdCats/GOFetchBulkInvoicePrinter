using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GOFetchBulkInvoicePrinter.Model
{
    public class Job
    {
        public JObject Assets { get; set; }
        public JObject OrderCart { get; set; }
        public JObject Order { get; set; }

        public string TrackingNo { get; set; }
        public string ReferenceInvoiceId { get; set; }
        public string PaymentMethod { get; set; }
        public string PickupBy { get; set; }
        public string DeliveryBy { get; set; }
        public string CashDeliveryBy { get; set; }
        public string PickupTime { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public string SubTotal { get; set; }
        public string ServiceCharge { get; set; }
        public string TotalToPay { get; set; }
        public string SpecialNotetoDeliveryMan { get; set; }

        public JToken[] PackageList { get; set; }
        public string Date { get; set; }
        public JToken[] Tasks { get; set; }
        public string DeliveryTime { get; internal set; }

        public List<PackageItem> PackageItemList { get; set; } = new List<PackageItem>();
    }

    public class PackageItem
    {
        public string Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public decimal Total { get; set; }
    }
}