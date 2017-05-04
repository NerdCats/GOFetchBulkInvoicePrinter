# GO!Fetch Bulk Invoice Printer
What is does:
This application is connected to the GO! Fetch back end service and by accepting bulk job ids in a text box seprated by comma or space it generates invoices for each job and sends them to the system's default printer and performs auto cutting in between the invoices.

# How it works:

1. The back end service endpoint is defined in the App.config file like this
```csharp
 <add name="GoFetchBulkInvoicePrinter.Properties.Settings.TaskCatAddress"
      connectionString="http://fetchprod.gobd.co" />
```
You can make this application hooked to any back end endpoint.

2. The default model for this application is 'Job' which is located in the 'Model' folder. You can modify it according to the payload you receive.

3. There is a default RDLC report named 'Invoice.rdlc' which is the printable invoice. You can customize it. Add more parameters and datasources.

4. There is a 'PrinterClass.cs' located in the 'ViewModel' folder which handles the direct printing of invoices. It has some basic funtions. In order to print a print an invoice you just have to call
```csharp
Run(DataTable reportDataTable, List<ReportParameter> reportParameters, string reportResource);
```
As mentioned above it expects 3 parameters.
