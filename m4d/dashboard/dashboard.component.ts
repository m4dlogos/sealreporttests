//#region Imports
import { Component, OnInit } from '@angular/core';
import { AngularTools } from 'src/app/tools/angular-tools';
import { DTO } from 'src/app/models/dto';
import { DashboardService } from 'src/app/services/dashboard.service';
import { Media4DisplayMobile } from 'src/app/models/media4display-mobile';
import { PercentageViewValue } from 'src/app/models/percentage-view';
import { TranslateService } from '@ngx-translate/core';
//#endregion

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent extends AngularTools.BaseComponent implements OnInit {

  //#region Propriétés

  static RoutePath: any;

  dashboardStatistics: DTO.DashboardStatistics = null;

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
   * Permet d'afficher le nombre de licences
   */
  public licensesCount = '';

  /**
   * Taille de la médiathèque
   */
  public librarySize: DTO.LibrarySize = null;

  /**
   * Libellé de la taille de la médiathèque
   */
  public librarySizeLabel = '';

  /**
   * Permet d'afficher la date d'expiration de la licence
   * TODO : récupérer la bonne valeur
   */
  public expirationDateLicence = '';

  /**
   * Permet de savoir si la date de licence est valide
   * TODO : récupérer la bonne valeur
   */
  public licenseDateIsValid = true;

  /**
   * Permet de récupérer la version de m4d
   * TODO : récupérer la bonne valeur
   */
  public m4dVersion = '';

  //#endregion

  //#region Méthodes

  /**
   * Constructeur
   */
  constructor(
    private dashboardService: DashboardService,
    translate: TranslateService
  ) {
    super(translate);
  }

  ngOnInit() {

    this.loadData();
  }

  /**
   * Récupère les statisiiques du parc de player
   */
  private loadData(): void {

    this.dashboardService
      .getDashboardStatistics()
      .subscribe((dashboardStatistics: DTO.DashboardStatistics) => {
        this.dashboardStatistics = dashboardStatistics;
        this.computeStats(dashboardStatistics);
      });

    this.dashboardService
      .getLibrarySize()
      .subscribe((librarySize: DTO.LibrarySize) => {
        this.librarySize = librarySize;
        this.computeLibrarySize(librarySize);
      });

    this.dashboardService
      .getM4DVersion()
      .subscribe((m4dVersion) => {
        this.m4dVersion = m4dVersion;
      });
  }

  /**
   * Permet de traiter les informations de la taille de la médiathèque
   */
  private computeLibrarySize(librarySize: DTO.LibrarySize): void {

    this.librarySize = librarySize;
    const size = this.convertSize(librarySize.contentSize, 2, this.translate);
    const capacity = librarySize.capacity > -1 ? `/ ${this.convertSize(librarySize.capacity, 2, this.translate)}` : '';
    this.librarySizeLabel = `${size} ${capacity}`;
  }

  /**
   * Permet de traiter les statistiques afin de fournir les pourcentages
   */
  private computeStats(dashboardStatistics: DTO.DashboardStatistics) {

    const green = '#10BE5D';
    const red = '#ea6153';
    const orange = '#FD990B';

    if (dashboardStatistics != null) {

      // .MB V.4.4.0 - 05/01/2018 - #7692 - Tableau de bord - Indicateur alarmes : masquer l'indicateur, mais garder le code au cas où
      const playersOkPercentage = this.getPercentage(
        dashboardStatistics.playersOkCount,
        dashboardStatistics.playersActivCount
      );
      const playersUnreachablePercentage = this.getPercentage(
        dashboardStatistics.playersUnreachableCount,
        dashboardStatistics.playersActivCount
      );
      const playersUpToDatePercentage = this.getPercentage(
        dashboardStatistics.playersUpToDateCount,
        dashboardStatistics.playersActivCount
      );
      const playersNotUpToDatePercentage = this.getPercentage(
        dashboardStatistics.playersNotUpToDateCount,
        dashboardStatistics.playersActivCount
      );

      // .MB V.4.4.0 - 05/01/2018 - #7660 - Affichage de la conformité dans le tableau de bord
      const playersConformPercentage = this.getPercentage(
        dashboardStatistics.playersConformCount,
        dashboardStatistics.playersActivCount
      );
      const playersNotConformPercentage = this.getPercentage(
        dashboardStatistics.playersNotConformCount,
        dashboardStatistics.playersActivCount
      );

      // .MB V.4.4.0 - 05/01/2018 - #7692 - Tableau de bord - Indicateur alarmes : masquer l'indicateur, mais garder le code au cas où
      // données état des players
      this.playersStatusValues = new Array<PercentageViewValue>();
      this.playersStatusValues.push({ color: green, percentage: playersOkPercentage });
      this.playersStatusValues.push({ color: orange, percentage: playersUnreachablePercentage });

      // données players à jour
      this.playersUpToDateValues = new Array<PercentageViewValue>();
      this.playersUpToDateValues.push({ color: green, percentage: playersUpToDatePercentage });
      this.playersUpToDateValues.push({ color: red, percentage: playersNotUpToDatePercentage });

      // .MB V.4.4.0 - 05/01/2018 - #7660 - Affichage de la conformité dans le tableau de bord
      // données players conformité
      this.playersConformityValues = new Array<PercentageViewValue>();
      this.playersConformityValues.push({ color: green, percentage: playersConformPercentage });
      this.playersConformityValues.push({ color: red, percentage: playersNotConformPercentage });

      this.licensesCount =
        dashboardStatistics.playerLicencesCount === -1 ?
          dashboardStatistics.playerIsInitialized.toString()
          : `${dashboardStatistics.playerIsInitialized} / ${dashboardStatistics.playerLicencesCount}`;

    }
  }

  /**
   * Permet d'obtenir le %
   * TODO : à mettre ailleurs
   */
  private getPercentage(itemCount: number, totalItemCount: number): number {

    let result = 0;

    if (totalItemCount > 0) {
      result = (itemCount * 100) / Math.max(1, totalItemCount);
    }

    return result;
  }

  /**
   * Permet de convertir la taille
   * TODO : à mettre ailleurs
   */
  private convertSize(bytes: number, decimalDigits: number, $translate: TranslateService = null): string {

    let result = '';

    if (bytes < 1024) {
      result = `${bytes} ${this.getFileSizeExtension(Media4DisplayMobile.FileSizeUnityEnum.octet, $translate)}`;
    } else if (bytes < 1048576) {
      const value = (bytes / 1024).toFixed(decimalDigits);
      result = `${value} ${this.getFileSizeExtension(Media4DisplayMobile.FileSizeUnityEnum.kiloOctet, $translate)}`;
    } else if (bytes < 1073741824) {
      const value = (bytes / 1048576).toFixed(decimalDigits);
      result = `${value} ${this.getFileSizeExtension(Media4DisplayMobile.FileSizeUnityEnum.megaOctet, $translate)}`;
    } else {
      const value = (bytes / 1073741824).toFixed(decimalDigits);
      result = `${value} ${this.getFileSizeExtension(Media4DisplayMobile.FileSizeUnityEnum.gigaOctet, $translate)}`;
    }

    return result;
  }

  /**
   * Permet de récupérer l'unité de taille de fichier dans la langue de l'utilisateur
   * TODO : à mettre ailleurs
   */
  private getFileSizeExtension(unity: Media4DisplayMobile.FileSizeUnityEnum, $translate: TranslateService) {

    let result: string = unity.toString();
    let key = '';

    switch (unity) {
      case Media4DisplayMobile.FileSizeUnityEnum.octet:
        key = 'PAGE_BYTES';
        break;
      case Media4DisplayMobile.FileSizeUnityEnum.kiloOctet:
        key = 'PAGE_KILO_BYTES';
        break;
      case Media4DisplayMobile.FileSizeUnityEnum.megaOctet:
        key = 'PAGE_MEGA_BYTES';
        break;
      case Media4DisplayMobile.FileSizeUnityEnum.gigaOctet:
        key = 'PAGE_GIGA_BYTES';
        break;
      default:
        break;
    }

    if (key !== '' && $translate != null) {
      result = $translate.instant(key);
      if (result === key) {
        result = unity.toString(); // fallback sur l'unité si la traduction n'est pas trouvée
      }
    }

    return result;
  }

  //#endregion
}
