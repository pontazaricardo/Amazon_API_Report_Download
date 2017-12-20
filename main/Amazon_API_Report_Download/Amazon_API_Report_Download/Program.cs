using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarketplaceWebService;
using MarketplaceWebService.Model;

namespace Amazon_API_Report_Download
{
    class Program
    {
        static void Main(string[] args)
        {

        }

        public static void DownloadSettlementReport(string merchantId, bool getOnlyMostRecent = false)
        {

        }


        /// <summary>
        /// Gets the last 3 settlement reports from Amazon API
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private List<Tuple<string,DateTime,DateTime>> getReportsIds(MarketplaceWebService.MarketplaceWebService service, GetReportListRequest request)
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
        private void invokeGetReport(MarketplaceWebService.MarketplaceWebService service, GetReportRequest request)
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
