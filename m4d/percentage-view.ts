import { AngularTools } from "app/tools/angular-tools";

/**
 * Classe permettant de passer les valeurs à afficher
 * @param {any} options
 */
export class PercentageViewValue {
   public percentage: number;
   public color: string;
}

export class PercentageViewColor {
   public static OK: string = "#2ecc71";
   public static KO: string = "#ea6153";
   public static GRAY: string = "rgb(179,179,179)";
   public static BLUE: string = "#4183d7";
}

/**
 * Classe permettant de dessiner une gauge pour afficher un pourcentage
 * @param {any} options
 * @returns
 */
export class PercentageView extends fabric.Rect {

   //#region variables

   public thickness: number = 10;
   public value: number = 0;
   public values: PercentageViewValue[] = [];
   public displayPercentage: boolean = true;
   public displayValue: boolean = true;
   public label: string = "";
   public fullCircle: boolean = false;

   //#endregion

   //#region constructeur

   /**
    * contructeur
    * @param {any} options
    */
   constructor(options: any) {

      super(options);

      this.thickness = options.thickness || 10;
      this.values = options.values || [];
      this.displayPercentage = options.displayPercentage !== undefined ? options.displayPercentage : true;
      this.displayValue = options.displayValue !== undefined ? options.displayValue : true;
      this.fullCircle = options.fullCircle !== undefined ? options.fullCircle : false;
      this.label = options.label || "";
   }

   //#endregion

   //#region méthodes

   /**
    * permet de récupérer des radians à partir de degré
    * @param {number} degree
    * @returns
    */
   public getRadianFromDegree(degree: number): number {

      return degree * Math.PI / 180;
   }

   /**
    * permet de faire le rendu
    * @param {CanvasRenderingContext2D} ctx
    */
   public render(ctx: CanvasRenderingContext2D) {

      const width = this.width;

      let fontSize = Math.floor(width * .3);
      ctx.font = fontSize + "px 'arial'";

      let left = (this.left + width / 2);
      let top = (this.top + width / 2);

      let fillColor: string = "#3498DB";
      let startAngle = 0;
      const valueForEndAngle: number = this.fullCircle ? 360 : 225;

      let endAngle: number = this.getRadianFromDegree(valueForEndAngle + 90);

      let percentage: number = 0;
      let color: string = "";

      if (this.displayPercentage) {

         let degree: number = 0;

         startAngle = this.getRadianFromDegree(90);
         endAngle = this.getRadianFromDegree(valueForEndAngle + 90);

         ctx.beginPath();
         ctx.arc(left, top, (width - this.thickness) / 2, startAngle, endAngle, false);
         ctx.strokeStyle = PercentageViewColor.GRAY; // "#F0F0F0";
         ctx.lineWidth = this.thickness;
         ctx.stroke();

         for (let i = 0; i < this.values.length; i++) {

            percentage = this.values[i].percentage;
            color = this.values[i].color;

            degree = ((valueForEndAngle * percentage) / 100);
            endAngle = startAngle + this.getRadianFromDegree(degree);

            ctx.beginPath();
            ctx.arc(left, top, (width - this.thickness) / 2, startAngle, endAngle, false);
            ctx.strokeStyle = color;
            ctx.lineWidth = this.thickness;
            ctx.stroke();

            startAngle = endAngle;
         }
      }
      if (this.displayValue) {
         this.value = this.values[0].percentage;
         fillColor = this.values[0].color;

         const text = parseInt(this.value + "", 10) + (this.displayPercentage ? "%" : "");
         fontSize = Math.floor(width * .2 * .75);
         let textWidth: number = ctx.measureText(text).width;

         left = this.left + ((width - textWidth) / 2);
         top = this.top + ((width + (fontSize / 1.6)) / 2);

         ctx.fillStyle = "#333333";
         ctx.fillText(text, left, top, width);

         const label = this.label;
         textWidth = ctx.measureText(text).width;
         left = 38;
         top = 90;
         ctx.font = "21px arial";
         ctx.fillText(label, left, top, width - left);
      }

   }

   //#endregion
}

/**
 * classe controlleur pour la directive percentage-view
 */
export class PercentageViewController extends AngularTools.BaseController {

   //#region variables

   /**
    * permet de savoir quand l'interface est prête
    */
   public ready: boolean = false;

   /**
    * permet de récupérer l'élément jquery de la page
    */
   public jqueryElement: ng.IAugmentedJQuery = null;

   /**
    * permet de récupérer le contrôle qui affiche le pourcentage
    */
   public percentageView: PercentageView = null;

   //#endregion

   //#region variable d'attributs

   /**
    * largeur du controlle
    */
   public canvasWidth: number;

   /**
    * pourcentage à afficher
    */
   public percentage: number;

   /**
    * Indique si l'on veut afficher les pourcentages
    */
   public displayPercentage: boolean;
   /**
    * Indique si l'on veut afficher la valeur
    */
   public displayValue: boolean;
   /**
    * titre à afficher
    */
   public graphTitle: string;

   /**
    * canvas
    */
   public canvas: fabric.ICanvas = null;

   /**
    * obtient ou définit la liste des valeurs à afficher
    */
   public percentageViewValues: PercentageViewValue[];

   /**
    * libellé
    */
   public label: string;

   /**
    * Indique si le cercle est à 360° ou non
    */
   public fullCircle: boolean;

   //#endregion

   //#region variables statiques

   public static values: string[] = [
      "canvasWidth",
      "percentageViewValues",
      "displayPercentage",
      "displayValue",
      "graphTitle",
      "label",
      "fullCircle"
   ];
   public static events: string[] = [];
   public static classId: string = "percentageViewController";
   public static alias: string = "percentageViewCtrl";
   public static template: string = "app/components/home/percentage-view.html";
   public static $inject: string[] = [AngularTools.GlobalValue.classId];

   //#endregion

   //#region constructeur

   /**
    * constructeur
    * @param {AngularTools.GlobalValue} globalValue
    */
   constructor(globalValue: AngularTools.GlobalValue) {
      super(globalValue);

   }
   //#endregion

   //#region méthodes

   /**
    * gestion de l'initialisation du contrôle
    */
   public onInit(): void {

      const canvasElement: HTMLCanvasElement = (this.jqueryElement.find("canvas"))[0] as HTMLCanvasElement;
      this.canvas = new fabric.Canvas(canvasElement, { backgroundColor: "transparent" } as any);

      const heightElement = this.canvasWidth;

      this.canvas.setWidth(this.canvasWidth);
      this.canvas.setHeight(heightElement);

      this.percentageView = new PercentageView({
			displayPercentage: this.displayPercentage,
			displayValue: this.displayValue,
			fullCircle: this.fullCircle,
			hasControls: false,
			height: heightElement,
			label: this.label,
			left: 0,
			lockMovementX: true,
			lockMovementY: true,
			selectable: true,
         top: 0,
			values: this.percentageViewValues,
         width: this.canvasWidth
      });

      this.canvas.add(this.percentageView);
      this.canvas.hoverCursor = "default";

   }

   //#endregion

}

/**
 * classe directive percentage-view
 */
export class PercentageViewDirective extends AngularTools.BaseDirective {

   //#region propriétés statiques

   public static classId: string = "percentageView";

   //#endregion

   //#region constructeur

   constructor() {
      super(
         [AngularTools.DirectiveType.ELEMENT],
         PercentageViewController.template,
         true,
         PercentageViewController.classId,
         PercentageViewController.alias,
         true,
         PercentageViewController.values,
         PercentageViewController.events
      );

      return super.getDirective(this);
   }

   //#endregion

   //#region méthodes

   public link = function($scope: ng.IScope, element: ng.IAugmentedJQuery, attr: ng.IAttributes, controller: PercentageViewController) {

      controller.jqueryElement = element;
      controller.onInit();
   };

   //#endregion

}
