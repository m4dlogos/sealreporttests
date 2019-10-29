using Seal.Converter;
using Seal.Model;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Telelogos.Reportings
{
   // Class that implements the dashboard report generation
   public class DashboardReportBuilder
   {
      protected Repository _repository;
      protected Report _report;

      public const string GREEN = "#10BE5D";
      public const string RED = "#EA6153";
      public const string ORANGE = "#FD990B";
      public const string COLUMN_STATISTIC = "Statistic";
      public const string COLUMN_VALUE = "Value";
      public const string MODEL_CONFORMITY = "Conformity Model";
      public const string MODEL_CONNECTION = "Connection Model";
      public const string MODEL_UPDATE = "Update Model";
      public const string STAT_CONFORM = "Conforme";
      public const string STAT_NOT_CONFORM = "Non conforme";
      public const string STAT_OK = "Connecté";
      public const string STAT_UNREACHABLE = "Injoignable";
      public const string STAT_UP_TO_DATE = "A jour";
      public const string STAT_NOT_UP_TO_DATE = "Non à jour";

      // The colors by model
      public static Dictionary<string, string> Colors = new Dictionary<string, string>
      {
         { MODEL_CONFORMITY, $"['{GREEN}', '{RED}']" },
         { MODEL_CONNECTION, $"['{GREEN}','{ORANGE}']" },
         { MODEL_UPDATE, $"['{GREEN}','{RED}']" }
      };

      // The statistics by model
      public static Dictionary<string, List<string>> Statistics = new Dictionary<string, List<string>>
      {
         { MODEL_CONFORMITY, new List<string> { STAT_CONFORM, STAT_NOT_CONFORM } },
         { MODEL_CONNECTION, new List<string> { STAT_OK, STAT_UNREACHABLE } },
         { MODEL_UPDATE, new List<string> { STAT_UP_TO_DATE, STAT_NOT_UP_TO_DATE } }
      };

      // Default constructor
      public DashboardReportBuilder()
      {
         CreateRepository();
      }

      // Create the repository with no sources
      public void CreateRepository()
      {
         _repository = Repository.Create();
         _repository.Sources.Clear();
      }

      // Build and returns the result table
      public DataTable BuildResultTable(DashboardStatistics statistics)
      {
         var resultTable = new DataTable();
         resultTable.Columns.Add(new DataColumn(COLUMN_STATISTIC, typeof(string)));
         resultTable.Columns.Add(new DataColumn(COLUMN_VALUE, typeof(int)));
         resultTable.Rows.Add(STAT_CONFORM, statistics.PlayersConformCount);
         resultTable.Rows.Add(STAT_NOT_CONFORM, statistics.PlayersNotConformCount);
         resultTable.Rows.Add(STAT_OK, statistics.PlayersOkCount);
         resultTable.Rows.Add(STAT_UNREACHABLE, statistics.PlayersUnreachableCount);
         resultTable.Rows.Add(STAT_UP_TO_DATE, statistics.PlayersUpToDateCount);
         resultTable.Rows.Add(STAT_NOT_UP_TO_DATE, statistics.PlayersNotUpToDateCount);

         return resultTable;
      }

      // Add the source to the repository as the default source
      public void AddSource(DataTable table)
      {
         if (_repository == null)
            CreateRepository();

         var source = MetaSource.Create(_repository);
         source.Name = "Telelogos Data Source";
         source.IsNoSQL = true;
         source.IsDefault = true;

         AddMasterTable(source, table);

         _repository.Sources.All(s => s.IsDefault = false);
         _repository.Sources.Add(source);
      }

      // Create the report
      public void CreateReport()
      {
         if (_repository == null)
            CreateRepository();

         _report = Report.Create(_repository);
         _report.DisplayName = "Dashboard Report";
         // Clear default objects created by SealReport
         _report.Views.Clear();
         _report.Models.Clear();
      }

      // Add the master table to the source from the input table
      protected void AddMasterTable(MetaSource source, DataTable table)
      {
         // Remove the master tables
         source.MetaData.Tables.RemoveAll(t => t.Alias == MetaData.MasterTableName);

         var master = MetaTable.Create();
         master.DynamicColumns = true;
         master.IsEditable = false;
         master.Alias = MetaData.MasterTableName;
         master.Source = source;

         foreach (DataColumn column in table.Columns)
         {
            var metaColumn = MetaColumn.Create(column.ColumnName);
            metaColumn.Source = source;
            metaColumn.DisplayName = Seal.Helpers.Helper.DBNameToDisplayName(column.ColumnName.Trim());
            metaColumn.Category = "Master";
            metaColumn.DisplayOrder = master.GetLastDisplayOrder();
            metaColumn.Type = Seal.Helpers.Helper.NetTypeConverter(column.DataType);
            metaColumn.SetStandardFormat();

            master.Columns.Add(metaColumn);
         }

         source.MetaData.Tables.Add(master);
      }

      // Get the master table
      protected MetaTable MasterTable
      {
         get
         {
            var src = _repository.Sources.FirstOrDefault(s => s.MetaData.MasterTable != null);
            if (src != null)
               return src.MetaData.MasterTable;

            return null;
         }
      }

      // Add the models
      public void AddModels()
      {
         AddModel(MODEL_CONFORMITY);
         AddModel(MODEL_CONNECTION);
         AddModel(MODEL_UPDATE);
      }

      // Fill the result table
      public void FillResultTable(DataTable table)
      {
         FillResult(MODEL_CONFORMITY, table);
         FillResult(MODEL_CONNECTION, table);
         FillResult(MODEL_UPDATE, table);
      }

      // Fill the result table for the model
      protected void FillResult(string modelName, DataTable table)
      {
         if (_report == null)
            CreateReport();

         var model = _report.Models.FirstOrDefault(m => m.Name == modelName);
         if (model != null)
         {
            var filter = GeModeltFilter(modelName);
            var resultTable = table.Clone();

            foreach (var row in table.Select(filter))
            {
               resultTable.ImportRow(row);
            }

            model.ResultTable = resultTable;
         }
      }

      // Returns the select statistics filter for the model
      protected string GeModeltFilter(string modelName)
      {
         var filter = COLUMN_STATISTIC + " in (";
         var stats = Statistics[modelName];
         for (int i = 0; i < stats.Count(); ++i)
         {
            filter += "'" + stats[i] + "'";
            if (i < stats.Count())
               filter += ",";
         }

         filter += ")";

         return filter;
      }

      // Add a model to the report
      protected void AddModel(string modelName)
      {
         if (_report == null)
            CreateReport();

         var model = _report.AddModel(false);
         model.Name = modelName;

         var master = this.MasterTable;
         var column = master.Columns.FirstOrDefault(c => c.Name == COLUMN_STATISTIC);
         if (column != null)
         {
            var element = ReportElement.Create();
            element.MetaColumnGUID = column.GUID;
            element.PivotPosition = PivotPosition.Row;
            element.SerieDefinition = SerieDefinition.Axis;
            element.SerieSortType = SerieSortType.None;
            element.SortOrder = SortOrderConverter.kNoSortKeyword;
            model.Elements.Add(element);
         }

         column = master.Columns.FirstOrDefault(c => c.Name == COLUMN_VALUE);
         if (column != null)
         {
            var element = ReportElement.Create();
            element.MetaColumnGUID = column.GUID;
            element.PivotPosition = PivotPosition.Data;
            element.ChartJSSerie = ChartJSSerieDefinition.Pie;
            element.SerieSortType = SerieSortType.None;
            element.SortOrder = SortOrderConverter.kNoSortKeyword;
            model.Elements.Add(element);
         }

         model.InitReferences();
      }

      // Add the views
      public virtual void AddViews()
      {
         if (_report == null)
            return;

         // Sanity clear
         _report.Views.Clear();

         var rootView = _report.AddRootView();
         rootView.SortOrder = _report.Views.Count > 0 ? _report.Views.Max(i => i.SortOrder) + 1 : 1;
         rootView.Name = Seal.Helpers.Helper.GetUniqueName("View", (from i in _report.Views select i.Name).ToList());

         var containerView = _report.AddChildView(rootView, "Container");
         containerView.InitParameters(false);
         containerView.Parameters.FirstOrDefault(p => p.Name == "grid_layout").Value = "col-sm-4;col-sm-4;col-sm-4";

         foreach (var model in _report.Models)
         {
            AddModelView(containerView, model);
         }
      }

      // Add the view for the model to the parent view
      protected void AddModelView(ReportView parentView, ReportModel model)
      {
         var modelView = _report.AddChildView(parentView, ReportViewTemplate.ModelName);
         // Remove the views created by the model template
         modelView.Views.Clear();
         modelView.Name = model.Name;
         modelView.ModelGUID = model.GUID;

         var chartJSView = _report.AddChildView(modelView, ReportViewTemplate.ChartJSName);

         chartJSView.InitParameters(false);
         chartJSView.GetParameter("chartjs_doughnut").BoolValue = true;
         chartJSView.GetParameter("chartjs_show_legend").BoolValue = true;
         chartJSView.GetParameter("chartjs_legend_position").TextValue = "bottom";
         chartJSView.GetParameter("chartjs_colors").Value = Colors[modelView.Name];
         chartJSView.GetParameter("chartjs_options_circumference").NumericValue = 225; // 1.25*PI
         chartJSView.GetParameter("chartjs_options_rotation").NumericValue = 90; // 0.5*PI
      }

      // Generate the report and returns the file path
      public string GenerateReport()
      {
         // Execute the report
         _report.RenderOnly = true;
         _report.Format = ReportFormat.html;
         var execution = new ReportExecution() { Report = _report };
         execution.Execute();
         while (_report.IsExecuting) System.Threading.Thread.Sleep(100);

         // Generate the report
         var outputFile = execution.GeneratePrintResult();
         return outputFile;
      }
   }
}
