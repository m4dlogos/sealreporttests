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
      protected Repository _repository;
      protected Report _report;
		protected DataTable _resultTable;

      public const string GREEN = "#10BE5D";
      public const string RED = "#EA6153";
      public const string ORANGE = "#FD990B";

      protected Dictionary<string, string> _colors;

		// Default constructor
		public DashboardReportBuilder()
		{
			CreateRepository();
			_colors = new Dictionary<string, string> { { "Conformite", $"['{GREEN}', '{RED}']" }, { "Connexion", $"['{GREEN}','{ORANGE}']" }, { "Maj", $"['{GREEN}','{RED}']" } };
		}

		// Create the repository with no sources
		public void CreateRepository()
      {
         _repository = Repository.Create();
         _repository.Sources.Clear();
      }

      // Add the source to the repository as the default source
      public void AddSource(DataTable table)
      {
			if (_repository == null)
				CreateRepository();

			_resultTable = table;

			var source = MetaSource.Create(_repository);
			source.Name = "Telelogos Data Source";
			source.IsNoSQL = true;
			source.IsDefault = true;

			AddMasterTable(source, table);

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
				metaColumn.DisplayName = Helper.DBNameToDisplayName(column.ColumnName.Trim());
				metaColumn.Category = "Master";
				metaColumn.DisplayOrder = master.GetLastDisplayOrder();
				metaColumn.Type = Helper.NetTypeConverter(column.DataType);
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
			AddModel("Conformite");
			AddModel("Connexion");
			AddModel("Maj");
		}

		// Fill the result table
		public void FillResultTable()
		{
			FillResultConformite(_resultTable);
			FillResultConnexion(_resultTable);
			FillResultMaj(_resultTable);
		}

		// Fill the result table of the conformity model
		protected void FillResultConformite(DataTable table)
		{
			if (_report == null)
				CreateReport();

			var model = _report.Models.FirstOrDefault(m => m.Name == "Conformite");
			if (model != null)
			{
				var resultTable = table.Clone();
				foreach (var row in table.Select("Indicateur in ('Conforme', 'Non conforme')"))
				{
					resultTable.ImportRow(row);
				}

				model.ResultTable = resultTable;
			}
		}

		// Fill the result table of the connection model
		protected void FillResultConnexion(DataTable table)
		{
			if (_report == null)
				CreateReport();

			var model = _report.Models.FirstOrDefault(m => m.Name == "Connexion");
			if (model != null)
			{
				var resultTable = table.Clone();
				foreach (var row in table.Select("Indicateur in ('Connecté', 'Injoignable')"))
				{
					resultTable.ImportRow(row);
				}

				model.ResultTable = resultTable;
			}
		}

		// Fill the result table of the up to date model
		protected void FillResultMaj(DataTable table)
		{
			if (_report == null)
				CreateReport();

			var model = _report.Models.FirstOrDefault(m => m.Name == "Maj");
			if (model != null)
			{
				var resultTable = table.Clone();
				foreach (var row in table.Select("Indicateur in ('A jour', 'Non à jour')"))
				{
					resultTable.ImportRow(row);
				}

				model.ResultTable = resultTable;
			}
		}

		// Add a model to the report
		protected void AddModel(string modelName)
      {
         if (_report == null)
            CreateReport();

         var model = _report.AddModel(false);
         model.Name = modelName;

			var master = this.MasterTable;
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
				return;

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
