using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarketplaceWebService;

namespace Amazon_API_Report_Download
{
    class Program
    {
        static void Main(string[] args)
        {

        }

        private List<Tuple<string,DateTime,DateTime>> getReportsIds(MarketplaceWebService.MarketplaceWebService service, MarketplaceWebService.Model.GetReportListRequest request)
        {
            List<Tuple<string, DateTime, DateTime>> reports = new List<Tuple<string, DateTime, DateTime>>(); //Includes the reportId, dateFrom and dateTo for a settlement report.

            if ((service == null) || (request == null))
            {
                return reports;
            }

        }
    }
}
