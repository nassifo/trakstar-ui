using System;
using System.Windows.Forms;
using System.Collections;
using ChartDirector;

namespace CSharpChartExplorer
{
    public partial class FrmRealTimeDemo : Form
    {
        // The data arrays that store the visible data. The data arrays are updated in realtime. In
        // this demo, we plot the last 100 samples.
        private const int sampleSize = 100;
        private DateTime[] timeStamps = new DateTime[sampleSize];
        private double[] dataSeriesA = new double[sampleSize];
        private double[] dataSeriesB = new double[sampleSize];
        private double[] dataSeriesC = new double[sampleSize];

        // In this demo, we use a data generator driven by a timer to generate realtime data. The
        // nextDataTime is an internal variable used by the data generator to keep track of which
        // values to generate next.
        private DateTime nextDataTime = DateTime.Now;

        public FrmRealTimeDemo()
        {
            InitializeComponent();
        }

        private void FrmRealTimeDemo_Load(object sender, EventArgs e)
        {
            // Data generation rate
            dataRateTimer.Interval = 250;

            // Chart update rate, which can be different from the data generation rate.
            chartUpdateTimer.Interval = (int)samplePeriod.Value;

            // Initialize data buffer to no data.
            for (int i = 0; i < timeStamps.Length; ++i)
                timeStamps[i] = DateTime.MinValue;

            // Enable RunPB button
            runPB.Checked = true;

            // Now can start the timers for data collection and chart update
            dataRateTimer.Start();
            chartUpdateTimer.Start();
        }

        //
        // The data update routine. In this demo, it is invoked every 250ms to get new data.
        //
        private void dataRateTimer_Tick(object sender, EventArgs e)
        {
            do
            {
                //
                // In this demo, we use some formulas to generate new values. In real applications,
                // it may be replaced by some data acquisition code.
                //
                double p = nextDataTime.Ticks / 10000000.0 * 4;
                double dataA = 20 + Math.Cos(p * 2.2) * 10 + 1 / (Math.Cos(p) * Math.Cos(p) + 0.01);
                double dataB = 150 + 100 * Math.Sin(p / 27.7) * Math.Sin(p / 10.1);
                double dataC = 150 + 100 * Math.Cos(p / 6.7) * Math.Cos(p / 11.9);

                // After obtaining the new values, we need to update the data arrays.
                shiftData(dataSeriesA, dataA);
                shiftData(dataSeriesB, dataB);
                shiftData(dataSeriesC, dataC);
                shiftData(timeStamps, nextDataTime);

                // Update nextDataTime. This is needed by our data generator. In real applications,
                // you may not need this variable or the associated do/while loop.
                nextDataTime = nextDataTime.AddMilliseconds(dataRateTimer.Interval);
            }
            while (nextDataTime < DateTime.Now);

            // We provide some visual feedback to the numbers generated, so you can see the
            // values being generated.
            valueA.Text = dataSeriesA[dataSeriesA.Length - 1].ToString(".##");
            valueB.Text = dataSeriesB[dataSeriesB.Length - 1].ToString(".##");
            valueC.Text = dataSeriesC[dataSeriesC.Length - 1].ToString(".##");
        }

        //
        // Utility to shift a double value into an array
        //
        private void shiftData(double[] data, double newValue)
        {
            for (int i = 1; i < data.Length; ++i)
                data[i - 1] = data[i];
            data[data.Length - 1] = newValue;
        }

        //
        // Utility to shift a DataTime value into an array
        //
        private void shiftData(DateTime[] data, DateTime newValue)
        {
            for (int i = 1; i < data.Length; ++i)
                data[i - 1] = data[i];
            data[data.Length - 1] = newValue;
        }
        
        //
        // Enable/disable chart update based on the state of the Run button.
        //
        private void runPB_CheckedChanged(object sender, EventArgs e)
        {
            chartUpdateTimer.Enabled = runPB.Checked;
        }

        //
        // Updates the chartUpdateTimer interval if the user selects another interval.
        //
        private void samplePeriod_ValueChanged(object sender, EventArgs e)
        {
            chartUpdateTimer.Interval = (int)samplePeriod.Value;
        }

        //
        // The chartUpdateTimer Tick event - this updates the chart periodicially by raising
        // viewPortChanged events.
        //
        private void chartUpdateTimer_Tick(object sender, EventArgs e)
        {
            winChartViewer1.updateViewPort(true, false);
        }

        //
        // The viewPortChanged event handler. In this example, it just updates the chart. If you
        // have other controls to update, you may also put the update code here.
        //
        private void winChartViewer1_ViewPortChanged(object sender, WinViewPortEventArgs e)
        {
            drawChart(winChartViewer1);
        }

        //
        // Draw the chart and display it in the given viewer.
        //
        private void drawChart(WinChartViewer viewer)
        {
            // Create a ThreeDScatterChart object of size 720 x 600 pixels
            ThreeDScatterChart c = new ThreeDScatterChart(720, 600);

            // Add a title to the chart using 20 points Times New Roman Italic font
            c.addTitle("3D Scatter Chart (1)  ", "Times New Roman Italic", 20);

            // Set the center of the plot region at (350, 280), and set width x depth
            // x height to 360 x 360 x 270 pixels
            c.setPlotRegion(350, 280, 360, 360, 270);

            // Add a scatter group to the chart using 11 pixels glass sphere symbols,
            // in which the color depends on the z value of the symbol
            c.addScatterGroup(dataSeriesC, dataSeriesB, dataSeriesA, "", Chart.GlassSphere2Shape, 11,
                Chart.SameAsMainColor);

            // Add a color axis (the legend) in which the left center is anchored at
            // (645, 270). Set the length to 200 pixels and the labels on the right
            // side.
            c.setColorAxis(645, 270, Chart.Left, 200, Chart.Right);

            // Set the x, y and z axis titles using 10 points Arial Bold font
            c.xAxis().setTitle("X-Axis Place Holder", "Arial Bold", 10);
            c.yAxis().setTitle("Y-Axis Place Holder", "Arial Bold", 10);
            c.zAxis().setTitle("Z-Axis Place Holder", "Arial Bold", 10);

            c.zAxis().setLinearScale(0, 130, 10);
            c.xAxis().setLinearScale(0, 250, 50, 10);
            c.yAxis().setLinearScale(0, 250, 50, 10);


            // Assign the chart to the WinChartViewer
            viewer.Chart = c;
        }
    }
}