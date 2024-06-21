
declare var heightData: any;
declare var heightString: string;
declare var weightData: any;
declare var weightString: string;

let measurementsHeightChartContainer = document.querySelector<HTMLCanvasElement>("#chart-container");
if (measurementsHeightChartContainer != null) {
    let myChart = new Chart(measurementsHeightChartContainer, {
        type: 'line',
        data: {
            datasets: [{
                label: heightString,
                data: heightData,
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(153, 102, 255, 1)',
                    'rgba(255, 159, 64, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                xAxes: [{
                    type: 'time',
                    time: {
                        displayFormats: {
                            quarter: 'MMM YYYY'
                        }
                    }
                }],
                yAxes: [{
                    ticks: {
                        beginAtZero: false
                    }
                }]
            }
        }
    });
}

let measurementsWeightChartContainer = document.querySelector<HTMLCanvasElement>("#chart-container2");
if (measurementsWeightChartContainer !== null) {
    var myChart2 = new Chart(measurementsWeightChartContainer, {
        type: 'line',
        data: {

            datasets: [{
                label: weightString,
                data: weightData,
                borderColor: 'rgb(95, 192, 192)',
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                xAxes: [{
                    type: 'time',
                    time: {
                        displayFormats: {
                            quarter: 'MMM YYYY'
                        }
                    }
                }],
                yAxes: [{
                    ticks: {
                        beginAtZero: false
                    }
                }]
            }
        }
    });
}
