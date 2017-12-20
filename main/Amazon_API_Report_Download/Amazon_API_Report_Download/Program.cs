using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarketplaceWebService;
using MarketplaceWebService.Model;

using System.Configuration;
using System.IO;


namespace Amazon_API_Report_Download
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Preparing application...");

            string accessKeyId = "<Your AWS Access Key>";
            string secretAccessKey = "<Your AWS Secret Key>";
            string applicationName = "<Your Application Name>";
            string applicationVersion = "<Your Application Version>";
            string merchantId = "<Your Merchant ID>";
            string marketplaceId = "<Your Marketplace ID>";

            MarketplaceWebServiceConfig config = new MarketplaceWebServiceConfig();
            config.ServiceURL = "https://mws.amazonservices.com";

            Console.WriteLine("Contacting Amazon web services...");

            config.SetUserAgentHeader(
                applicationName,
                applicationVersion,
                "C#",
                "<Parameter 1>", "<Parameter 2>");
            MarketplaceWebService.MarketplaceWebService service = new MarketplaceWebServiceClient(accessKeyId, secretAccessKey, config);

            Console.WriteLine("Service client created. Downloading settlement report...");

            DownloadSettlementReport(service, merchantId);

            Console.Read();
        }

        public static void DownloadSettlementReport(MarketplaceWebService.MarketplaceWebService service, string merchantId, bool getOnlyMostRecent = false)
        {
            GetReportListRequest request = new GetReportListRequest();
            request.Merchant = merchantId;

            TypeList reportTypes = new TypeList();
            reportTypes.Type = new List<string>() { "_GET_V2_SETTLEMENT_REPORT_DATA_FLAT_FILE_V2_" };

            request.ReportTypeList = reportTypes;

            List<Tuple<string, DateTime, DateTime>> settlemementReportsInfo = getReportsIds(service, request);

            if (settlemementReportsInfo.Count() == 0)
            {
                Console.WriteLine("No reports downloaded. Exiting.");
                return;
            }

            int selectedReport = 0;

            if (!getOnlyMostRecent)
            {
                Console.WriteLine("List of available reports:");
                for (int i = 0; i < 3; i++)
                {
                    int index = i + 1;
                    Console.WriteLine(index + ". " + settlemementReportsInfo[i].Item2.ToShortDateString() + " - " + settlemementReportsInfo[i].Item3.ToShortDateString());
                }

                Console.WriteLine("Select option:");
                char userInput = (char)Console.Read();
                Console.ReadLine();
                if (userInput == '1' || userInput == '2' || userInput == '3')
                {
                    switch (userInput)
                    {
                        case '2':
                            selectedReport = 1;
                            break;
                        case '3':
                            selectedReport = 2;
                            break;
                        case '1':
                        default:
                            selectedReport = 0;
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid option detected. Selecting most recent report as default."); //At this point selectedReport = 0;
                }
            }

            string reportId = settlemementReportsInfo[selectedReport].Item1;

            #region Downloading report

            GetReportRequest requestReport = new GetReportRequest();
            requestReport.Merchant = merchantId;
            requestReport.ReportId = reportId;

            string amzSettlementReportPath = ConfigurationManager.AppSettings["Amz.SettlementReport.DownloadPath"]; //File path directory. Example: "C:/"
            string amzSettlementReportFile = amzSettlementReportPath + "amzSettlementReport-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";  //NOTE: The invokeGetReport needs to get all the time a different file name to avoid having an MD5 error. (Problem found while developing. Solution found here: https://sellercentral.amazon.com/forums/thread.jspa?threadID=12563)

            requestReport.Report = File.Open(amzSettlementReportFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            invokeGetReport(service, requestReport); //Added timestamp to the report filename to avoid the MD5 problem from Amazon's side.

            Console.WriteLine("File was downloaded to: " + amzSettlementReportFile);

            #endregion
        }


        /// <summary>
        /// Gets the last 3 settlement reports from Amazon API
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static List<Tuple<string,DateTime,DateTime>> getReportsIds(MarketplaceWebService.MarketplaceWebService service, GetReportListRequest request)
        {
            List<Tuple<string, DateTime, DateTime>> reports = new List<Tuple<string, DateTime, DateTime>>(); //Includes the reportId, dateFrom and dateTo for a settlement report.

            if ((service == null) || (request == null))
            {
                return reports;
            }

            try
            {
                List<string> reportIds = new List<string>();
                List<DateTime> availableDates = new List<DateTime>();

                GetReportListResponse response = service.GetReportList(request);

                if (response.IsSetGetReportListResult())
                {
                    GetReportListResult getReportListResult = response.GetReportListResult;
                    List<ReportInfo> reportInfoList = getReportListResult.ReportInfo;
                    foreach (ReportInfo reportInfo in reportInfoList)
                    {
                        reportIds.Add(reportInfo.ReportId);
                        availableDates.Add(reportInfo.AvailableDate);
                    }
                }

                if (availableDates.Count < 4)
                {
                    //We were not able to get the last 3 reports. We will try then to return the most recent one.
                    string reportId = reportIds[0];
                    DateTime dateFrom = availableDates[1].AddDays(-1); //Settlement reports are available only until the next day of their period.
                    DateTime dateTo = availableDates[0].AddDays(-1);

                    Tuple<string, DateTime, DateTime> reportInfo = Tuple.Create(reportId, dateFrom, dateTo);
                    reports.Add(reportInfo);
                }else
                {
                    //We have more than 3 reports.
                    for(int i = 0; i < 4; i++)
                    {
                        if (DateTime.Now.Subtract(availableDates[i]).Days > 2)  //We exclude the last report if it is still open (open = haven't finished the actual period).
                        {
                            string reportId = reportIds[i];
                            DateTime dateFrom = availableDates[i + 1].AddDays(-1); //Settlement reports are available only until the next day of their period.
                            DateTime dateTo = availableDates[i].AddDays(-1);

                            Tuple<string, DateTime, DateTime> reportInfo = Tuple.Create(reportId, dateFrom, dateTo);
                            reports.Add(reportInfo);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Cannot download the list of settlement reports. Exception: " + e.Message);
            }

            return reports;
        }

        /// <summary>
        /// The GetReport operation returns the contents of a report. Reports can potentially be
        /// very large (>100MB) which is why we only return one report at a time, and in a
        /// streaming fashion.
        /// 
        /// </summary>
        /// <param name="service">Instance of MarketplaceWebService service</param>
        /// <param name="request">GetReportRequest request</param>
        private static void invokeGetReport(MarketplaceWebService.MarketplaceWebService service, GetReportRequest request)
        {
            try
            {
                Console.WriteLine("Downloading report...");

                GetReportResponse response = service.GetReport(request);

                Console.WriteLine("Downloaded!");

            }
            catch (MarketplaceWebServiceException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Please try again in a couple of minutes.");  //Exception might be because of Amazon's limit of downloading a report once a minute. Try in a couple of min.
            }
        }
    }
}
