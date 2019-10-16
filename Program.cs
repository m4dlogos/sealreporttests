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
         Console.WriteLine("Choisir votre format de sortie:");
         Console.WriteLine("1. HTML");
         Console.WriteLine("2. Print");
         Console.WriteLine("3. Pdf");
         int num = 0;
         var ok = int.TryParse(Console.ReadLine(), out num);
         var options = new List<int> { 1, 2, 3 };

         while (options.Contains(num) && ok)
         {
            var format = ReportFormat.html;
           switch (num)
            {
               case 1: format = ReportFormat.html; break;
               case 2: format = ReportFormat.print; break;
               case 3: format = ReportFormat.pdf; break;
            }

            ConformityReport(DATA_Statistics, format);

            Console.WriteLine("Générer un autre rapport ? (choisir le format ou appuyer sur une autre touche)\n");
            ok = int.TryParse(Console.ReadLine(), out num);
         }
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

      public static void ConformityReport(DashboardStatistics data, ReportFormat format)
      {
         // Create the repository
         var repository = Repository.Create();

         // Create No Sql data source
         var source = MetaSource.Create(repository);
         source.Name = "Telelogos NoSql Data Source";
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
         report.DisplayName = "Rapport de conformité des players";

         // Configure the model's elements
         var model = report.Models[0];
         // Add the result table
         model.ResultTable = new DataTable();

         model.ResultTable.Columns.Add(new DataColumn("Conformite", typeof(string)));
         model.ResultTable.Columns.Add(new DataColumn("Quantite", typeof(int)));

         model.ResultTable.Rows.Add("Conforme", data.PlayersConformCount);
         model.ResultTable.Rows.Add("Non conforme", data.PlayersNotConformCount);

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

            if (column.Name == "Conformite")
            {
               element.PivotPosition = PivotPosition.Row;
               element.SerieDefinition = SerieDefinition.Axis;
            }
            else if (column.Name == "Quantite")
            {
               element.PivotPosition = PivotPosition.Data;
               element.ChartJSSerie = ChartJSSerieDefinition.Pie;
            }

            report.Models[0].Elements.Add(element);
         }

         source.InitReferences(repository);
         model.InitReferences();

         // Configure the view
         var view = report.Views.Find(x => x.ViewName == "View");
         var viewModel = view.Views.Find(x => x.ViewName == "Model");
         var container = viewModel.Views.Find(x => x.ViewName == "Model Container");
         var chartJS = container.Views.Find(x => x.ViewName == "Chart JS");
         chartJS.InitParameters(false);
         var paramTitle = chartJS.Parameters.Find(x => x.Name == "chartjs_title");
         paramTitle.TextValue = "Conformité des players";
         var paramDoughnut = chartJS.Parameters.Find(x => x.Name == "chartjs_doughnut");
         paramDoughnut.BoolValue = true;

         // Execute the report
         report.RenderOnly = true;
         report.Format = ReportFormat.pdf;
         //report.Views[0].PdfConfigurations.Add(getPdfHeaderConfiguration());
         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         //while (report.IsExecuting) System.Threading.Thread.Sleep(100);

         // Generate the report
         var outputFile = execution.GeneratePDFResult();
         sendEmail(outputFile);

         // Show the report
         Process.Start(outputFile);
      }

      static DashboardStatistics DATA_Statistics = new DashboardStatistics
      {
         PlayersOkCount = 1,
         PlayersUnreachableCount = 0,
         PlayersActivCount = 1,
         PlayersUpToDateCount = 1,
         PlayersNotUpToDateCount = 0,
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
