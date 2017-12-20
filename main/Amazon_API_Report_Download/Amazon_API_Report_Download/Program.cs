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

            }
            catch(Exception e)
            {

            }
        }
    }
}
