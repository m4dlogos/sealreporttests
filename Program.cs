using Seal.Converter;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;

namespace TelelogosGenerationReport
{
   class Program
   {
      static void Main(string[] args)
      {
         GenerateConformityReport(DATA_Statistics, ReportFormat.html);
      }

      #region Samples
      public static void SimpleExecution()
      {
         //Simple load and report execution and generation in a HTML Result file
         Repository repository = Repository.Create();
         Report report = Report.LoadFromFile(@"C:\Users\as\Documents\Etudes\SealReport\Seal-Report-5.0\Repository\Reports\M4D_Conformite.srex", repository);
         report.DisplayName = "Rapport de conformité des players";
         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);
         string result = execution.GenerateHTMLResult();
         
         Process.Start(result);
      }

      public static void CreationAndExecution()
      {
         var repository = Repository.Create();
         Report report = Report.Create(repository);
         report.DisplayName = "Meetings Report : Creation an Execution";
         var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("StatisticsDataSource"));
         source.MetaData.Tables.Clear();

         //Update the data source with the table dbo.Rooms
         var table = source.AddTable(true);
         table.Name = "t";
         table.Sql = "select r.ID_ROOM as Id, r.ROOM_NAME as Name, m.MEETINGS_COUNT as NumberOfMeetings, " +
                     "m.OCCUPATION_RATE as OccupationRate, m.NUMBER_OF_ATTENDEES as NumberOfAttendees " +
                     "from dbo.Rooms r inner join dbo.Meetings m on r.ID_ROOM = m.ID_ROOM";
         table.Refresh();

         //Set the source of the default model
         report.Models[0].SourceGUID = source.GUID;
         //Add elements to the reports model
         foreach (var column in table.Columns)
         {
            var element = ReportElement.Create();
            element.MetaColumnGUID = column.GUID;
            element.Name = column.Name.Replace("t.", "");
            
            switch (element.Name)
            {
               case "Id": element.PivotPosition = PivotPosition.Hidden; break;
               case "Name":
                  {
                     element.PivotPosition = PivotPosition.Row;
                     element.SerieDefinition = SerieDefinition.Axis;
                  }
                  break;
               default:
                  {
                     element.PivotPosition = PivotPosition.Data;
                     element.ChartJSSerie = ChartJSSerieDefinition.Bar;
                  }
                  break;
            }
            
            element.Source = source;
            report.Models[0].Elements.Add(element);
         }


         var view = report.Views.Find(x => x.ViewName == "View");
         var model = view.Views.Find(x => x.ViewName == "Model");
         var container = model.Views.Find(x => x.ViewName == "Model Container");
         var chartJS = container.Views.Find(x => x.ViewName == "Chart JS");

         chartJS.InitParameters(false);
         var paramTitle = chartJS.Parameters.Find(x => x.Name == "chartjs_title");
         paramTitle.TextValue = "Number of meetings by room";
         var paramBarH = chartJS.Parameters.Find(x => x.Name == "chartjs_bar_horizontal");
         paramBarH.BoolValue = true;
                 

         //Then execute it
         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);
         string result = execution.GenerateHTMLResult();

         Process.Start(result);
      }

      #endregion

      public const string RootViewName = "View";

      public static void AddModel(string modelName, Report report, MetaTable master, DashboardStatistics data)
      {
			var model = report.AddModel(false);
			model.Name = modelName;
			model.ResultTable = GetResultTable(modelName, data);

			foreach (var column in master.Columns)
			{
				var element = ReportElement.Create();
				element.MetaColumnGUID = column.GUID;

				switch (column.Name)
				{
					case "Nom":
						{
							element.PivotPosition = PivotPosition.Row;
							element.SerieDefinition = SerieDefinition.Axis;
							element.SerieSortType = SerieSortType.None;
							element.SortOrder = SortOrderConverter.kNoSortKeyword;
						}
						break;
					case "Valeur":
						{
							element.PivotPosition = PivotPosition.Data;
							element.ChartJSSerie = ChartJSSerieDefinition.Pie;
							element.SerieSortType = SerieSortType.None;
							element.SortOrder = SortOrderConverter.kNoSortKeyword;
						}
						break;
				}

				model.Elements.Add(element);
			}

			model.InitReferences();
		}

      public static DataTable GetResultTable(string modelName, DashboardStatistics data)
      {
			var resultTable = CreateResultTable();

         if (modelName == "Conformite")
         {
            resultTable.Rows.Add("Conforme", data.PlayersConformCount);
            resultTable.Rows.Add("Non conforme", data.PlayersNotConformCount);
         }
         else if (modelName == "Connexion")
         {
            resultTable.Rows.Add("Connecté", data.PlayersOkCount);
            resultTable.Rows.Add("Injoignable", data.PlayersUnreachableCount);
         }
         else if (modelName == "Maj")
         {
            resultTable.Rows.Add("A jour", 10);
            resultTable.Rows.Add("Non à jour", 11);
         }

			return resultTable;
      }

		public static MetaSource CreateSource(Repository repository)
		{
			// Create No Sql data source
			var source = MetaSource.Create(repository);
			source.Name = "Telelogos Data Source";
			source.IsNoSQL = true;
			source.IsDefault = true;
			foreach (var src in repository.Sources) src.IsDefault = false;

			repository.Sources.Add(source);

			return source;
		}												

		public static MetaTable CreateMasterTable(MetaSource source, DataTable table)
		{
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

			return master;
		}

		public static DataTable CreateResultTable()
		{
			var table = new DataTable();

			table.Columns.Add(new DataColumn("Nom", typeof(string)));
			table.Columns.Add(new DataColumn("Valeur", typeof(int)));
			return table;
		}

		public static Report CreateReport(Repository repository)
		{
			var report = Report.Create(repository);
			report.DisplayName = "Dashboard Report";
			// Vider les models et les vues
			report.Views.RemoveRange(0, report.Views.Count);
			report.Models.RemoveRange(0, report.Models.Count);

			return report;
		}

		public static void AddViews(Report report)
		{
			var rootView = report.AddRootView();
			rootView.SortOrder = report.Views.Count > 0 ? report.Views.Max(i => i.SortOrder) + 1 : 1;
			rootView.Name = Helper.GetUniqueName("View", (from i in report.Views select i.Name).ToList());

			var containerView = report.AddChildView(rootView, "Container");
			containerView.InitParameters(false);
			containerView.Parameters.FirstOrDefault(p => p.Name == "grid_layout").Value = "col-sm-4;col-sm-4;col-sm-4";

			foreach (var model in report.Models)
			{
				var modelView = report.AddChildView(containerView, ReportViewTemplate.ModelName);
				modelView.Views.RemoveRange(0, modelView.Views.Count); // Supprimer les vues par défaut ajoutées lorsque c'est le template Model
				modelView.Name = model.Name;
				modelView.ModelGUID = model.GUID;

				var chartJSView = report.AddChildView(modelView, ReportViewTemplate.ChartJSName);

				chartJSView.InitParameters(false);
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_doughnut").BoolValue = true;
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_show_legend").BoolValue = true;
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_legend_position").TextValue = "bottom";
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_colors").Value = GetColor(model.Name);
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_options_circumference").Value = "1.25*Math.PI";
				chartJSView.Parameters.FirstOrDefault(p => p.Name == "chartjs_options_rotation").Value = "0.5*Math.PI";
			}
		}

		public static string GetColor(string modelName)
		{
			if (modelName == "Conformite")
			{
				return "['#10BE5D','#ea6153']";
			}
			else if (modelName == "Connexion")
			{
				return "['#10BE5D','orange']";
			}
			else if (modelName == "Maj")
			{
				return "['#10BE5D','#ea6153']";
			}

			return string.Empty;
		}

      public static void GenerateConformityReport(DashboardStatistics data, ReportFormat format)
      {
			// Create the repository
			var repository = Repository.Create();
			// Create the NoSql source
			var source = CreateSource(repository);
			// Create the result DataTable
			var resultTable = CreateResultTable();
			// Create the master table and add it to the data source
			var master = CreateMasterTable(source, resultTable);
			// Create the report
			var report = CreateReport(repository);
			// Add models
			AddModel("Conformite", report, master, data);
			AddModel("Connexion", report, master, data);
			AddModel("Maj", report, master, data);
			// Add views
			AddViews(report);

          // Execute the report
          report.RenderOnly = true;
          report.Format = format;
          //report.Views[0].PdfConfigurations.Add(getPdfHeaderConfiguration());
          var execution = new ReportExecution() { Report = report };
          execution.Execute();
          while (report.IsExecuting) System.Threading.Thread.Sleep(100);
          
          // Generate the report
          var outputFile = execution.GeneratePrintResult();
          //sendEmail(outputFile);

          // Show the report
          Process.Start(outputFile);
      }

      static DashboardStatistics DATA_Statistics = new DashboardStatistics
      {
         PlayersOkCount = 1,
         PlayersUnreachableCount = 20,
         PlayersActivCount = 80,
         PlayersUpToDateCount = 70,
         PlayersNotUpToDateCount = 30,
         PlayersConformCount = 66,
         PlayersNotConformCount = 34,
         PlayerIsInitialized = 2,
         PlayerLicencesCount = -1
      };

      private static string getPdfHeaderConfiguration()
      {
         var configurationPath = Path.GetDirectoryName(Repository.Instance.ConfigurationPath);
         return File.ReadAllText(Path.Combine(configurationPath, "PdfHeaderOptions.xml"));
      }

      static void sendEmail(string file)
      {
         try
         {
            var device = OutputEmailDevice.Create();
            device.Server = "smtp3";
            device.Port = 25;
            device.UserName = "";
            device.Password = "";
            device.SenderEmail = "aseguin@telelogos.com";

            var from = "aseguin@telelogos.com";
            var to = "aseguin@telelogos.com";
            //MailMessage message = new MailMessage(from, to);
            //message.From = new MailAddress(Helper.IfNullOrEmpty(from, device.SenderEmail));
            //device.AddEmailAddresses(message.To, to);
            //message.Subject = "M4D - Rapport de conformité du " + DateTime.Now.ToLongDateString();
            //message.Body = "Test d'envoie du rapport de conformité";
            //message.Attachments.Insert(0, new Attachment(file));

            //device.SmtpClient.Send(message);
            MailMessage message = new MailMessage(from, to);
            message.Subject = "M4D - Rapport de conformité du " + DateTime.Now.ToLongDateString();
            message.Body = "Test d'envoie du rapport de conformité";
            var smtp = new SmtpClient(device.Server, device.Port);
            smtp.Send(message);
         }
         catch (Exception emailEx)
         {
            Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error got trying sending notification email.\r\n{0}", emailEx.Message);
         }
      }
   }
}
