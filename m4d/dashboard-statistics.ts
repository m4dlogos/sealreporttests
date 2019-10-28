import { PercentageViewValue } from "app/components/home/percentage-view";
import { DTO } from "app/models/dto";
import { Media4Display } from "app/models/media4display";
import { ConsoleUsersService } from "app/services/console-users-service";
import { M4DLocalStorageService } from "app/services/m4d-local-storage-service";
import { PlayersService } from "app/services/players-service";
import { AngularTools } from "app/tools/angular-tools";

/**
 * Classe contrôleur pour la directive dashboard-statistics
 */
export class DashboardStatisticsController extends AngularTools.BaseController {

   //#region variables

   public canvas: fabric.ICanvas = null;

   /**
    * statistiques
    */
   public dashboardStatistics: DTO.DashboardStatistics = null;

   /**
    * données sur les statuts des players
    */
   public playersStatusValues: PercentageViewValue[] = new Array<PercentageViewValue>();

   /**
    * données sur l'état à jour des players
    */
   public playersUpToDateValues: PercentageViewValue[] = new Array<PercentageViewValue>();

   /**
    * données sur la conformité des players
    */
   public playersConformityValues: PercentageViewValue[] = new Array<PercentageViewValue>();

   /**
    * texte du nombre de licences
    */
   public licenseCountText: string = "";

   /**
    * permet de  savoir si l'interface est prête à l'affichage
    */
   public ready: boolean = false;

   /**
    * permet de savoir si l'utilisateur est playerManager
    */
   public isPlayerManager: boolean = true;

   /**
    * nom du filtre de la liste des players en session
    */
   private playerListIdSession: string;

   // obtient ou définit la méthode de chargement des données
   private loadDataDeffered: ng.IDeferred<DTO.DashboardStatistics>;

   //#endregion

   //#region variables statiques

   public static values: string[] = [];
   public static events: string[] = [];
   public static classId: string = "dashboardStatisticsController";
   public static alias: string = "dashboardStatisticsCtrl";
   public static template: string = "app/components/home/dashboard-statistics.html";
   public static $inject: string[] = [
      AngularTools.GlobalValue.classId,
      PlayersService.classId,
      ConsoleUsersService.classId,
      M4DLocalStorageService.classId,
      "$uibModal",
      "$state",
      "$translate",
      "$scope",
      "$window",
      "$cookies",
      "$filter"];

   //#endregion

   //#region constructeur

   /**
    * constructeur
    * @param {AngularTools.GlobalValue} globalValue
    * @param {PlayersService} private playersService
    */
   constructor(
      globalValue: AngularTools.GlobalValue,
      private playersService: PlayersService,
      private consoleUsersService: ConsoleUsersService,
      private m4dLocalStorageService: M4DLocalStorageService,
      private $uibModal: ng.ui.bootstrap.IModalService,
      private $state: ng.ui.IStateService,
      private $translate: ng.translate.ITranslateService,
      private $scope: ng.IScope,
      private $window: ng.IWindowService,
      private $cookies: ng.cookies.ICookiesService,
      public $filter: ng.IFilterService) {
      super(globalValue);

      this.playerListIdSession = Media4Display.FormCamelCaseToUnderscoreUpperCase("Players") + "_LIST_COL_FILTER_" + Media4Display.ListId.PLAYERS;

      // on supprime la mise à jour de l'interface en sortie de page
      this.$scope.$on("$destroy", () => {

         if (!this.ready && this.loadDataDeffered != null) {
            this.cancelDefer(this.loadDataDeffered);
         }
      });
   }

   //#endregion

   //#region méthodes

   public getPercentage(itemCount: number, totalItemCount: number): number {

      let result: number = 0;

      if (totalItemCount > 0) {
         result = (itemCount * 100) / Math.max(1, totalItemCount);
      }

      return result;
   }

   /**
    * gestion de l'initialisation de la page
    */
   public onInit(): void {

      // on charge les données depuis le players-service
      this.loadDataDeffered = this.playersService.getDashboardStatistics();

      this.loadDataDeffered.promise.then((dashboardStatistics: DTO.DashboardStatistics) => {

         // voir _variables.scss
         const green = "#10BE5D";
         const red = "#ea6153";
         const orange = "#FD990B";

         this.dashboardStatistics = dashboardStatistics;

         if (this.dashboardStatistics != null) {

            const playersOkPercentage = this.getPercentage(
               dashboardStatistics.playersOkCount,
               dashboardStatistics.playersActivCount);

            const playersUnreachablePercentage = this.getPercentage(
               dashboardStatistics.playersUnreachableCount,
               dashboardStatistics.playersActivCount);

            const playersUpToDatePercentage = this.getPercentage(
               dashboardStatistics.playersUpToDateCount,
               dashboardStatistics.playersActivCount);

            const playersNotUpToDatePercentage = this.getPercentage(
               dashboardStatistics.playersNotUpToDateCount,
               dashboardStatistics.playersActivCount);

            // .MB V.4.4.0 - 05/01/2018 - #7660 - Affichage de la conformité dans le tableau de bord
            const playersConformPercentage = this.getPercentage(
               dashboardStatistics.playersConformCount,
               dashboardStatistics.playersActivCount);

            const playersNotConformPercentage = this.getPercentage(
               dashboardStatistics.playersNotConformCount,
               dashboardStatistics.playersActivCount);

            // on transforme les données afin d'alimenter les graphiques
            // données état des players
            this.playersStatusValues = new Array<PercentageViewValue>();
            this.playersStatusValues.push({ color: green, percentage: playersOkPercentage });
            this.playersStatusValues.push({ color: orange, percentage: playersUnreachablePercentage });
            // self.playersStatusValues.push({ color: red, percentage: playersNotOkPercentage });

            // données players à jour
            this.playersUpToDateValues = new Array<PercentageViewValue>();
            this.playersUpToDateValues.push({ color: green, percentage: playersUpToDatePercentage });
            this.playersUpToDateValues.push({ color: red, percentage: playersNotUpToDatePercentage });

            // .MB V.4.4.0 - 05/01/2018 - #7660 - Affichage de la conformité dans le tableau de bord
            // données players conformité
            this.playersConformityValues = new Array<PercentageViewValue>();
            this.playersConformityValues.push({ color: green, percentage: playersConformPercentage });
            this.playersConformityValues.push({ color: red, percentage: playersNotConformPercentage });

            // données licence
            this.licenseCountText = dashboardStatistics.playerLicencesCount === -1 ?
               dashboardStatistics.playerIsInitialized.toString()
               : dashboardStatistics.playerIsInitialized + " / " + dashboardStatistics.playerLicencesCount;
         }

         // on récupère la fait que l'utilisateur soit gestionnaire de player
         this.consoleUsersService.isPlayerManager().then((isPlayerManager: boolean) => {

            this.isPlayerManager = isPlayerManager;
            this.ready = true;
         });

      }).finally(() => {
         this.$scope.$root.$emit(AngularTools.BaseService.ONHOMEPAGELOADDONE, AngularTools.BaseService.DASHBOARDLOADED);
      });

   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut ok
    */
   public onViewPlayersOk(): void {
      this.openPlayersPage("connected", "-1");
   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut injoignable
    */
   public onViewPlayersUnreachable(): void {
      this.openPlayersPage("connected", "0");
   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut à jour
    */
   public onViewPlayersUpToDate(): void {
      this.openPlayersPage("upToDate", "-1");
   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut non à jour
    */
   public onViewPlayersNotUpToDate(): void {
      this.openPlayersPage("upToDate", "0");
   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut conforme
    */
   public onViewPlayersConform(): void {
      this.openPlayersPage("conformity", "-1");
   }

   /**
    * gestion de la demande d'ouverture de la page des players au statut non conforme
    */
   public onViewPlayersNotConform(): void {
      this.openPlayersPage("conformity", "0");
   }

   /**
    * demande d'ouverture de la page de liste des players
    * @param {string} filter
    * @param {string | boolean} filterValue
    */
   private openPlayersPage(filterName: string, filterValue: string | boolean): void {

      this.m4dLocalStorageService.removePlayerListViewNode();

      const filter: DTO.ColumnFilter = new DTO.ColumnFilter();
      filter.name = filterName;
      filter.operator = "=";
      filter.value = filterValue;
      filter.type = Media4Display.ColumnFilterType.STANDARD;

      const columnFilters: DTO.ColumnFilter[] = [];
      columnFilters.push(filter);
      this.$window.localStorage.setItem(this.playerListIdSession, angular.toJson(columnFilters));

      // yh 24/01/2018 on force l'affichage en mode list
      this.$cookies.put("PlayersDetailedDisplay", Media4Display.PlayersDisplayMode.LIST.toString(), { expires: moment().add(50, Media4Display.DateType.YEAR).toDate() });
      this.$state.go(AngularTools.RouteKey.PLAYERS_LIST);
   }
   //#endregion
}

/**
 * Classe directive pour dashboard-directive
 */
export class DashboardStatisticsDirective extends AngularTools.BaseDirective {

   //#region propriétés statiques

   public static classId: string = "dashboardStatistics";

   //#endregion

   //#region constructeur

   constructor() {
      super(
         [AngularTools.DirectiveType.ELEMENT],
         DashboardStatisticsController.template,
         true,
         DashboardStatisticsController.classId,
         DashboardStatisticsController.alias,
         true,
         DashboardStatisticsController.values,
         DashboardStatisticsController.events);

      return super.getDirective(this);
   }

   //#endregion
}
