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
      private DataTable _resultTable;

      // Constructor
      public DashboardReportBuilderManager()
      {
         BuildResultTable();
      }

      // Build the result table
      private void BuildResultTable()
      {
         _resultTable = new DataTable();
         _resultTable.Columns.Add(new DataColumn("Indicateur", typeof(string)));
         _resultTable.Columns.Add(new DataColumn("Valeur", typeof(int)));
      }

      // Add the conformity model
      private void AddConformiteModel(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
         var table = _resultTable.Clone();
         table.Rows.Add("Conforme", statistics.PlayersConformCount);
         table.Rows.Add("Non conforme", statistics.PlayersNotConformCount);

         builder.AddModel("Conformite", table);
      }

      // Add the model for devices connections
      private void AddConnexionModel(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
         var table = _resultTable.Clone();
         table.Rows.Add("Connecté", statistics.PlayersOkCount);
         table.Rows.Add("Injoignable", statistics.PlayersUnreachableCount);

         builder.AddModel("Connexion", table);
      }

      // Add the model for devices up to date 
      private void AddMajModel(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
         var table = _resultTable.Clone();
         table.Rows.Add("A jour", statistics.PlayersUpToDateCount);
         table.Rows.Add("Non à jour", statistics.PlayersNotUpToDateCount);

         builder.AddModel("Maj", table);
      }

      // Build the report
      public void BuildReport(DashboardReportBuilder builder, DashboardStatistics statistics)
      {
         builder.SetReporDisplaytName("Dashboard Report");
         // Add models
         AddConformiteModel(builder, statistics);
         AddConnexionModel(builder, statistics);
         AddMajModel(builder, statistics);
         // Add views
         builder.AddViews();
      }
   }
}
