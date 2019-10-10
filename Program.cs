using Seal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelelogosGenerationReport
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("1. Simple Execution");
         Console.WriteLine("2. Creation adn Execution");
         Console.WriteLine("3. M4D Percent View");
         Console.WriteLine("Which report ? (1, 2 or 3)");
         var num = int.Parse(Console.ReadLine());

         while (num != 0)
         {
           switch (num)
            {
               case 1: SimpleExecution(); break;
               case 2: CreationAndExecution(); break;
               case 3: NoSql(); break;
            }

            Console.WriteLine("Génération du rapport terminée ... (0 to quit)\n");
            num = int.Parse(Console.ReadLine());
         }
      }

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

      public static void NoSql()
      {
         // Create the repository
         var repository = Repository.Create();

         // Create No Sql data source
         var source = MetaSource.Create(repository);
         source.Name = "Telelogos NoSql Data Source";
         source.IsNoSQL = true;
         source.IsDefault = true;
         foreach (var src in repository.Sources) src.IsDefault = false;

         // Create the master table and add it to the data source
         var master = MetaTable.Create();
         master.DynamicColumns = true;
         master.IsEditable = false;
         master.Alias = MetaData.MasterTableName;
         master.Source = source;
         source.MetaData.Tables.Add(master);

         // Add definition script and refresh the table
         master.DefinitionScript = File.ReadAllText(Path.Combine(Repository.Instance.ViewsFolder, "Telelogos", "M4D_Conformite_Definition.cshtml"));
         master.Refresh();

         // Add the source to the repository
         source.InitReferences(repository);
         repository.Sources.Add(source);

         // Create the report
         Report report = Report.Create(repository);
         report.DisplayName = "Rapport de conformité des players";

         // Configure the model's elements
         var model = report.Models[0];

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

         model.InitReferences();

         // Add the data for the reporting
         model.ResultTable = model.Source.MetaData.MasterTable.NoSQLTable.Clone();
         model.ResultTable.Rows.Add("Conforme", 65);
         model.ResultTable.Rows.Add("Non conforme", 35);

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
         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);

         // Generate the HTML
         string result = execution.GenerateHTMLResult();
         // Show the report
         Process.Start(result);
      }

      public static void NoSQLDataSource()
      {
         var repository = Repository.Create();
         Report report = Report.Create(repository);
         report.DisplayName = "Players Reporting";
         

         var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("NoSQLDataSourceWith"));
         


         report.Models[0].SourceGUID = source.GUID;

         var element = ReportElement.Create();
         var column = source.MetaData.MasterTable.Columns.First(c => c.Name == "Conformite");
         element.Name = column.Name;
         element.MetaColumnGUID = column.GUID;
         element.MetaColumn = column;
         element.Model = report.Models[0];
         element.PivotPosition = PivotPosition.Row;
         element.SerieDefinition = SerieDefinition.Axis;
         report.Models[0].Elements.Add(element);

         element = ReportElement.Create();
         column = source.MetaData.MasterTable.Columns.First(c => c.Name == "Quantite");
         element.Name = column.Name;
         element.MetaColumnGUID = column.GUID;
         element.MetaColumn = column;
         element.Model = report.Models[0];
         report.Models[0].Elements.Add(element);
         element.PivotPosition = PivotPosition.Data;
         element.ChartJSSerie = ChartJSSerieDefinition.Pie;
         report.Models[0].Elements.Add(element);

         var resTable = new DataTable();

         resTable.Columns.Add(new DataColumn("Conformite", typeof(string)));
         resTable.Columns.Add(new DataColumn("Quantite", typeof(int)));
         resTable.Rows.Add("Conforme", 75);
         resTable.Rows.Add("Non conforme", 25);
         report.Models[0].ResultTable = resTable;
         report.RenderOnly = true;


         var view = report.Views.Find(x => x.ViewName == "View");
         var model = view.Views.Find(x => x.ViewName == "Model");
         var container = model.Views.Find(x => x.ViewName == "Model Container");
         var chartJS = container.Views.Find(x => x.ViewName == "Chart JS");

         chartJS.InitParameters(false);
         var paramTitle = chartJS.Parameters.Find(x => x.Name == "chartjs_title");
         paramTitle.TextValue = "Conformité des players";
         var paramDoughnut = chartJS.Parameters.Find(x => x.Name == "chartjs_doughnut");
         paramDoughnut.BoolValue = true;

         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);
         string result = execution.GenerateHTMLResult();

         Process.Start(result);
      }

      public static void M44D_PercentView(int companyId)
      {
         var repository = Repository.Create();
         Report report = Report.Create(repository);
         report.DisplayName = "Players Reporting";
         var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("NoSQLDataSource"));

         //source.MetaData.MasterTable.DefinitionScript


         var table = new DataTable();
         table.Columns.Add(new DataColumn("Conformite", typeof(string)));
         table.Columns.Add(new DataColumn("Quantite", typeof(int)));

         table.Rows.Add("Conforme", 75);
         table.Rows.Add("Non conforme", 25);

         source.MetaData.MasterTable.NoSQLTable = table;

         report.Models[0].SourceGUID = source.GUID;

         var element = ReportElement.Create();
         var column = table.Columns["Conformite"];
         element.Name = column.ColumnName;
         element.MetaColumn = MetaColumn.Create("Conformite");
         element.PivotPosition = PivotPosition.Row;
         element.SerieDefinition = SerieDefinition.Axis;
         report.Models[0].Elements.Add(element);

         element = ReportElement.Create();
         column = table.Columns["Quantite"];
         element.Name = column.ColumnName;
         element.MetaColumn = MetaColumn.Create("Quantite");
         report.Models[0].Elements.Add(element);
         element.PivotPosition = PivotPosition.Data;
         element.ChartJSSerie = ChartJSSerieDefinition.Pie;

         report.Models[0].FillResultTable();



         var view = report.Views.Find(x => x.ViewName == "View");
         var model = view.Views.Find(x => x.ViewName == "Model");
         var container = model.Views.Find(x => x.ViewName == "Model Container");
         var chartJS = container.Views.Find(x => x.ViewName == "Chart JS");

         chartJS.InitParameters(false);
         var paramTitle = chartJS.Parameters.Find(x => x.Name == "chartjs_title");
         paramTitle.TextValue = "Conformité des players";
         var paramDoughnut = chartJS.Parameters.Find(x => x.Name == "chartjs_doughnut");
         paramDoughnut.BoolValue = true;

         ReportExecution execution = new ReportExecution() { Report = report };
         execution.Execute();
         while (report.IsExecuting) System.Threading.Thread.Sleep(100);
         string result = execution.GenerateHTMLResult();

         Process.Start(result);
      }

      internal static  DashboardStatistics getData()
      {
         return new DashboardStatistics()
         {
            PlayersOkCount = 1,
            PlayersUnreachableCount = 0,
            PlayersActivCount = 1,
            PlayersUpToDateCount = 1,
            PlayersNotUpToDateCount = 0,
            PlayersConformCount = 1,
            PlayersNotConformCount = 0,
            PlayerIsInitialized = 2,
            PlayerLicencesCount = -1
         };
      }
   }
}
