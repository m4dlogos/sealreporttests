using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

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

      public static void GenerateConformityReport(DashboardStatistics data, ReportFormat format)
      {
         // Create the repository
         var repository = Repository.Create();

         // Create No Sql data source
         var source = MetaSource.Create(repository);
         source.Name = "Telelogos Data Source";
         source.IsNoSQL = true;
         source.IsDefault = true;
         foreach (var src in repository.Sources) src.IsDefault = false;

         repository.Sources.Add(source);

         // Create the master table and add it to the data source
         var master = MetaTable.Create();
         master.DynamicColumns = true;
         master.IsEditable = false;
         master.Alias = MetaData.MasterTableName;
         master.Source = source;
         source.MetaData.Tables.Add(master);      

         // Create the report
         Report report = Report.Create(repository);
         report.DisplayName = "Dashboard Report";

         // Configure the model's elements
         var model = report.Models[0];
         // Add the result table
         model.ResultTable = new DataTable();

         model.ResultTable.Columns.Add(new DataColumn("Indicateur", typeof(string)));
         model.ResultTable.Columns.Add(new DataColumn("Nom", typeof(string)));
         model.ResultTable.Columns.Add(new DataColumn("Valeur", typeof(int)));

         model.ResultTable.Rows.Add("Conformité", "Conforme", data.PlayersConformCount);
         model.ResultTable.Rows.Add("Conformité", "Non conforme", data.PlayersNotConformCount);
         model.ResultTable.Rows.Add("Connexion", "Connecté", data.PlayersActivCount);
         model.ResultTable.Rows.Add("Connexion", "Injoignable", data.PlayersUnreachableCount);
         model.ResultTable.Rows.Add("Mise à jour", "A jour", data.PlayersUpToDateCount);
         model.ResultTable.Rows.Add("Mise à jour", "Non à jour", data.PlayersNotUpToDateCount);

         foreach (DataColumn column in model.ResultTable.Columns)
         {
            var metaColumn = MetaColumn.Create(column.ColumnName);
            metaColumn.Source = master.Source;
            metaColumn.DisplayName = Helper.DBNameToDisplayName(column.ColumnName.Trim());
            metaColumn.Category = "Master";
            metaColumn.DisplayOrder = master.GetLastDisplayOrder();
            metaColumn.Type = Helper.NetTypeConverter(column.DataType);
            metaColumn.SetStandardFormat();
            master.Columns.Add(metaColumn);
         }

         foreach (var column in master.Columns)
         {
            var element = ReportElement.Create();
            element.MetaColumnGUID = column.GUID;

            switch (column.Name)
            {
               case "Indicateur":
                  {
                     element.PivotPosition = PivotPosition.Page;
                  }
                  break;
               case "Nom":
                  {
                     element.PivotPosition = PivotPosition.Row;
                     element.SerieDefinition = SerieDefinition.Axis;
                  }
                  break;
               case "Valeur":
                  {
                     element.PivotPosition = PivotPosition.Data;
                     element.ChartJSSerie = ChartJSSerieDefinition.Pie;
                  }
                  break;
            }

            report.Models[0].Elements.Add(element);
         }

         source.InitReferences(repository);
         model.InitReferences();

         string rootViewName = "View";
         // Remove no used views
         var sqlView = report.Views.FirstOrDefault(v => v.Name == "SQL view");
         if (sqlView != null)
            report.Views.Remove(sqlView);

         // Configure the view
         var view = report.Views.FirstOrDefault(p => p.ViewName == rootViewName);
         var viewModel = view?.Views.FirstOrDefault(p => p.ViewName == ReportViewTemplate.ModelName);
         var viewModelContainer = viewModel?.Views.FirstOrDefault(p => p.ViewName == ReportViewTemplate.ModelContainerName);
         viewModelContainer.Views.RemoveAll(v => v.Template.Name != "Test");
         report.AddChildView(viewModelContainer, "Test");
         var viewChartJS = viewModelContainer?.Views.FirstOrDefault(p => p.ViewName == ReportViewTemplate.ChartJSName);
         if (viewChartJS != null)
         {
            viewChartJS.InitParameters(false);
            viewChartJS.Parameters.FirstOrDefault(p => p.Name == "chartjs_doughnut").BoolValue = true;
            viewChartJS.Parameters.FirstOrDefault(p => p.Name == "chartjs_legend_position").TextValue = "bottom";
         }

         // Execute the report
         report.RenderOnly = true;
         report.Format = format;
         //report.Views[0].PdfConfigurations.Add(getPdfHeaderConfiguration());
         var execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);

         // Paramétrer la vue Model
         viewModel.Parameters.FirstOrDefault(p => p.Name == "show_summary_table").BoolValue = false;
         viewModel.Parameters.FirstOrDefault(p => p.Name == "show_page_tables").BoolValue = false;
         viewModel.Parameters.FirstOrDefault(p => p.Name == "show_data_tables").BoolValue = false;
         viewModel.Parameters.FirstOrDefault(p => p.Name == "show_page_separator").BoolValue = false;
         viewModel.Parameters.FirstOrDefault(p => p.Name == "pages_layout").TextValue = "col-sm-4;col-sm-4;col-sm-4";
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
