using Seal.Converter;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelelogosGenerationReport
{
   // Class that implements the dashboard report generation
   public class DashboardReportBuilder
   {
      public const string RootViewName = "View";

      protected Repository _repository;
      protected MetaSource _source;
      protected Report _report;

      public const string GREEN = "#10BE5D";
      public const string RED = "#EA6153";
      public const string ORANGE = "#FD990B";

      protected Dictionary<string, string> _colors;

      // Create the repository with no sources
      protected virtual void CreateRepository()
      {
         _repository = Repository.Create();
         _repository.Sources.Clear();
      }

      // Create a default source
      protected virtual void CreateSource()
      {
         if (_repository == null)
            CreateRepository();

         _source = MetaSource.Create(_repository);
         _source.Name = "Telelogos Data Source";
         _source.IsNoSQL = true;
         _source.IsDefault = true;
         _repository.Sources.All(s => s.IsDefault = false);
         _repository.Sources.Add(_source);
      }

      // Create an empty report
      protected virtual void CreateReport()
      {
         if (_repository == null)
            CreateRepository();

         _report = Report.Create(_repository);
         _report.DisplayName = "Telelogos Report";
         // Clear default objects created by SealReport
         _report.Views.Clear();
         _report.Models.Clear();
      }

      // Default constructor
      public DashboardReportBuilder()
      {
         CreateRepository();
         CreateSource();
         CreateReport();
         _colors = new Dictionary<string, string> { { "Conformite", $"['{GREEN}', '{RED}']" }, { "Connexion", $"['{GREEN}','{ORANGE}']" }, { "Maj", $"['{GREEN}','{RED}']" } };
      }

      // Set the display name of the report
      public void SetReporDisplaytName(string name)
      {
         if (_report == null)
            CreateReport();

         _report.DisplayName = name;
      }

      // Create the master table from the DataTable which is the definition of the result table and which is used to configure the reporting model
      protected void CreateMasterTable(DataTable table)
      {
         if (_source == null)
            CreateSource();

         // Remove the master table
         _source.MetaData.Tables.RemoveAll(t => t.Alias == MetaData.MasterTableName);

         var master = MetaTable.Create();
         master.DynamicColumns = true;
         master.IsEditable = false;
         master.Alias = MetaData.MasterTableName;
         master.Source = _source;

         foreach (DataColumn column in table.Columns)
         {
            var metaColumn = MetaColumn.Create(column.ColumnName);
            metaColumn.Source = _source;
            metaColumn.DisplayName = Helper.DBNameToDisplayName(column.ColumnName.Trim());
            metaColumn.Category = "Master";
            metaColumn.DisplayOrder = master.GetLastDisplayOrder();
            metaColumn.Type = Helper.NetTypeConverter(column.DataType);
            metaColumn.SetStandardFormat();
            master.Columns.Add(metaColumn);
         }

         _source.MetaData.Tables.Add(master);
      }

      // Indicates whether the source has a master table or not
      public bool HasMasterTable { get => _source != null && _source.MetaData.Tables.FirstOrDefault(t => t.Alias == MetaData.MasterTableName) != null; }

      // Add a model to the report
      public virtual void AddModel(string modelName, DataTable resultTable)
      {
         if (_source == null)
            CreateSource();
         if (_report == null)
            CreateReport();
         if (!this.HasMasterTable)
            CreateMasterTable(resultTable);

         var model = _report.AddModel(false);
         model.Name = modelName;
         model.ResultTable = resultTable;

         var master = _source.MetaData.Tables.FirstOrDefault(t => t.Alias == MetaData.MasterTableName);
         var column = master.Columns.FirstOrDefault(c => c.Name == "Indicateur");
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

         column = master.Columns.FirstOrDefault(c => c.Name == "Valeur");
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
            CreateReport();
         // Sanity clear
         _report.Views.Clear();

         var rootView = _report.AddRootView();
         rootView.SortOrder = _report.Views.Count > 0 ? _report.Views.Max(i => i.SortOrder) + 1 : 1;
         rootView.Name = Helper.GetUniqueName("View", (from i in _report.Views select i.Name).ToList());

         var containerView = _report.AddChildView(rootView, "Container");
         containerView.InitParameters(false);
         containerView.Parameters.FirstOrDefault(p => p.Name == "grid_layout").Value = "col-sm-4;col-sm-4;col-sm-4";

         foreach (var model in _report.Models)
         {
            var modelView = _report.AddChildView(containerView, ReportViewTemplate.ModelName);
            // Remove the views created by the model template
            modelView.Views.Clear();
            modelView.Name = model.Name;
            modelView.ModelGUID = model.GUID;

            var chartJSView = _report.AddChildView(modelView, ReportViewTemplate.ChartJSName);

            chartJSView.InitParameters(false);
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_doughnut").BoolValue = true;
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_show_legend").BoolValue = true;
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_legend_position").TextValue = "bottom";
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_colors").Value = _colors[modelView.Name];
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_options_circumference").Value = "1.25*Math.PI";
            chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_options_rotation").Value = "0.5*Math.PI";
         }
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
