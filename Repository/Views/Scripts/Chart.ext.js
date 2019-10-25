// Création d'un nouveau modèle qui hérite du modèle doughnut
Chart.defaults.doughnutWithText = Chart.defaults.doughnut;

Chart.controllers.doughnutWithText = Chart.controllers.doughnut.extend({
	// Draw the representation of the dataset
	// @param ease : if specified, this number represents how far to transition elements. See the implementation of draw() in any of the provided controllers to see how this should be used
	draw: function(ease) {
		Chart.controllers.doughnut.prototype.draw.call(this, ease);

		var ctx = this.chart.chart.ctx;
		ctx.save();

		ctx.font = Chart.helpers.fontString(Chart.defaults.global.defaultFontFamily, 'normal', Chart.defaults.global.defaultFontFamily);
		ctx.textAlign = 'center';
		ctx.textBaseline = 'bottom';
		this.chart.data.datasets.forEach(function (dataset) {

		for (var i = 0; i < dataset.data.length; i++) {
			var model = dataset._meta[Object.keys(dataset._meta)[0]].data[i]._model,
				total = dataset._meta[Object.keys(dataset._meta)[0]].total,
				mid_radius = model.innerRadius + (model.outerRadius - model.innerRadius)/2,
				start_angle = model.startAngle,
				end_angle = model.endAngle,
				mid_angle = start_angle + (end_angle - start_angle)/2;

				var x = mid_radius * Math.cos(mid_angle);
				var y = mid_radius * Math.sin(mid_angle);

				ctx.fillStyle = '#fff';
				if (i == 3){ // Darker text color for lighter background
					ctx.fillStyle = '#444';
				}
				//var percent = String(Math.round(dataset.data[i]/total*100)) + "%";      
				//Don't Display If Legend is hide or value is 0
				if(dataset.data[i] != 0 && dataset._meta[Object.keys(dataset._meta)[0]].data[i].hidden != true) {
					ctx.fillText(dataset.data[i], model.x + x, model.y + y);
					// Display percent in another line, line break doesn't work for fillText
					//ctx.fillText(percent, model.x + x, model.y + y + 15);
				}
			}
		});

		ctx.font = '52px arial'; // '48px sherif';
		ctx.fillStyle = 'black';
		ctx.textBaseline = 'middle';
		var model = this.chart.data.datasets[0]._meta[Object.keys(this.chart.data.datasets[0]._meta)[0]].data[0]._model;
		var total = this.chart.data.datasets[0].data[0] + this.chart.data.datasets[0].data[1];
		var conforme = this.chart.data.datasets[0].data[0];
		var percent = Number(conforme/total).toLocaleString(undefined,{style: 'percent', minimumFractionDigits:0});
		ctx.fillText(percent +" " + this.chart.data.labels[0], this.chart.width/2 + (model.outerRadius - model.innerRadius), this.chart.height/2 + model.innerRadius/3 , 2*model.innerRadius);

		ctx.restore();
	},

	// Create elements for each piece of data in the dataset. Store elements in an array on the dataset as dataset.metaData
	addElements: function() {
		Chart.controllers.doughnut.prototype.addElements.call(this);
	},

	// Create a single element for the data at the given index and reset its state
	addElementAndReset: function(index) {
		Chart.controllers.doughnut.prototype.addElementAndReset.call(this, index);
	},

	// Remove hover styling from the given element
	removeHoverStyle: function(element) {
		Chart.controllers.doughnut.prototype.removeHoverStyle.call(this, element);
	},

	// Add hover styling to the given element
	setHoverStyle: function(element) {
		Chart.controllers.doughnut.prototype.setHoverStyle.call(this, element);
	},

	// Update the elements in response to new data
	// @param reset : if true, put the elements into a reset state so they can animate to their final values
	update: function(reset) {
		Chart.controllers.doughnut.prototype.update.call(this, reset);
	}
});