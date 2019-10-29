using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelelogosGenerationReport
{
   // Class that handles the dashreport report builder
   public class DashboardReportBuilderManager
   {
      // Constructor
      public DashboardReportBuilderManager()
      {
      }

      // Build the report
      public void BuildReport(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
			var resultTable = builder.BuildResultTable(statistics);
			builder.CreateRepository();
			builder.AddSource(resultTable);
			builder.CreateReport();
			builder.AddModels();
         builder.AddViews();
			builder.FillResultTable(resultTable);
      }
   }
}
