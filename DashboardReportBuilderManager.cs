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

      // Build the result table
      private DataTable BuildResultTable(DashboardStatistics statistics)
      {
         var resultTable = new DataTable();
			resultTable.Columns.Add(new DataColumn("Indicateur", typeof(string)));
			resultTable.Columns.Add(new DataColumn("Valeur", typeof(int)));
			resultTable.Rows.Add("Conforme", statistics.PlayersConformCount);
			resultTable.Rows.Add("Non conforme", statistics.PlayersNotConformCount);
			resultTable.Rows.Add("Connecté", statistics.PlayersOkCount);
			resultTable.Rows.Add("Injoignable", statistics.PlayersUnreachableCount);
			resultTable.Rows.Add("A jour", statistics.PlayersUpToDateCount);
			resultTable.Rows.Add("Non à jour", statistics.PlayersNotUpToDateCount);

			return resultTable;
		}

      // Build the report
      public void BuildReport(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
			var table = BuildResultTable(statistics);
			builder.CreateRepository();
			builder.AddSource(table);
			builder.CreateReport();
			builder.AddModels();
         builder.AddViews();
			builder.FillResultTable();
      }
   }
}
