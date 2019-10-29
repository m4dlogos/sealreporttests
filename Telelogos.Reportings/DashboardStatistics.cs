using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telelogos.Reportings
{
   public class DashboardStatistics
   {
      /// <summary>
      /// Obtient ou définit le nombre de players non suspendus
      /// </summary>
      public int PlayersActivCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players ok
      /// </summary>
      public int PlayersOkCount { get; set; }

      // <summary>
      /// Obtient ou définit le nombre de players injoignables
      /// </summary>
      public int PlayersUnreachableCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de licences
      /// </summary>
      public int PlayerLicencesCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players ayant une alarme
      /// </summary>
      public int PlayersWithAlarmCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players à jour
      /// </summary>
      public int PlayersUpToDateCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players non à jour
      /// </summary>
      public int PlayersNotUpToDateCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players sans alarme
      /// </summary>
      public int PlayersWithoutAlarmCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players conforme
      /// </summary>
      public int PlayersConformCount { get; set; }

      /// <summary>
      /// Obtient ou définit le nombre de players non conforme
      /// </summary>
      public int PlayersNotConformCount { get; set; }

      /// <summary>
      /// Obtient ou défini le nombre de players initialisés
      /// </summary>
      public int PlayerIsInitialized { get; set; }
   }
}
