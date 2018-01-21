using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using ChartDirector;
using TrakstarInterface;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace TrakstarGUI
{
    public partial class RealTimeDemoWindow : Window
    {
        private const int bufferSize = 500;
        private DateTime[] timeStamps = new DateTime[bufferSize];

        private List<SensorBuffer> dataBufferList = new List<SensorBuffer>();

        // Instance of the Trakstar
        private Trakstar bird = new Trakstar();

        // Date timer to keep track of when to sample bird again, the timer interval is the sampling
        // frequency of our device
        private DispatcherTimer dataRateTimer = new DispatcherTimer(DispatcherPriority.Render);

        // Timer used to updated the chart
        private DispatcherTimer chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Render);

        public RealTimeDemoWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Data generation rate = bird.SamplingRate (100Hz or 10ms by default)
            dataRateTimer.Interval = new TimeSpan(0, 0, 0, 0, bird.GetSamplingRate());
            dataRateTimer.Tick += dataRateTimer_Tick;
            
            // Chart update rate, which can be different from the data generation rate.
            chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(samplePeriod.Text));
            chartUpdateTimer.Tick += chartUpdateTimer_Tick;

            // Initialize data buffer to no data.
            for (int i = 0; i < timeStamps.Length; ++i)
                timeStamps[i] = DateTime.MinValue;

            // Enable RunPB button
            runPB.IsChecked = true;

            // Initialize buffer list
            for (int i = 0; i < bird.GetNumberOfSensors(); i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    // Initialize empty buffer
                    double[] dataBuffer = new double[bufferSize];

                    SensorBuffer sensorBuffer = new SensorBuffer();

                    sensorBuffer.id = i;
                    sensorBuffer.coordinate = j;
                    sensorBuffer.buffer = dataBuffer;

                    dataBufferList.Add(sensorBuffer);
                }
            }

            // Now can start the timers for data collection and chart update
            dataRateTimer.Start();
            chartUpdateTimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bird.TrakstarOff();
        }

        //
        // The data update routine. Every bird.SamplingRate, get a new data record of all the sensors
        //
        private async void dataRateTimer_Tick(object sender, EventArgs e)
        {
            // Get all the new data records for all sensors and all degrees of freedom (x,y,z,a,e,o)
            var records = await bird.FetchDataAsync();

            if (records.Length > 0)
            {
                foreach (var dataBuffer in dataBufferList)
                {
                    // After obtaining the new values, we need to update the data arrays.
                    shiftData(dataBuffer.buffer, bird.getCoordinateFromRecords(records, dataBuffer.id, dataBuffer.coordinate)); // Shift in new sensor data;                  
                }

                shiftData(timeStamps, DateTime.Now); // Add time stamp to mark the time we retreived the data

                // Write data to file
                using (StreamWriter outputFile = new StreamWriter("records.txt", append: true))
                {
                    for (int i = 0; i < records.Length; i++)
                    {
                        await outputFile.WriteLineAsync(i + ", " + records[i].x + ", " + records[i].y + ", " + records[i].z + ", " + records[i].time);
                    }
                }
            }           
        }

        //
        // The chartUpdateTimer Tick event - this updates the chart periodicially by raising
        // viewPortChanged events.
        //
        private void chartUpdateTimer_Tick(object sender, EventArgs e)
        {
            SensorDataChart.updateViewPort(true, false);
        }

        //
        // Enable/disable chart update based on the state of the Run button.
        //
        private void runPB_CheckedChanged(object sender, RoutedEventArgs e)
        {
            chartUpdateTimer.IsEnabled = runPB.IsChecked == true;
        }

        //
        // Updates the chartUpdateTimer interval if the user selects another interval.
        //
        private void samplePeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedText = (samplePeriod.SelectedValue as ComboBoxItem).Content as string;
            if (!string.IsNullOrEmpty(selectedText))
                chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(selectedText));
        }

        //
        // The viewPortChanged event handler. In this example, it just updates the chart. If you
        // have other controls to update, you may also put the update code here.
        //
        private void SensorDataChart_ViewPortChanged(object sender, WPFViewPortEventArgs e)
        {
            drawChart(sender as WPFChartViewer);
        }

        //
        // Draw the chart and display it in the given viewer.
        //
        private void drawChart(WPFChartViewer viewer)
        {
            // Create an XYChart object 600 x 270 pixels in size, with light grey (f4f4f4) 
            // background, black (000000) border, 1 pixel raised effect, and with a rounded frame.
            XYChart c = new XYChart((int) (0.8*TrakstarUIWindow.ActualWidth), (int) (0.8* TrakstarUIWindow.ActualHeight), 0xf4f4f4, 0x000000, 1);
            
            c.setPlotArea(55, 62, (int)(0.8*TrakstarUIWindow.ActualWidth), (int)(0.65*TrakstarUIWindow.ActualHeight), 0xffffff, -1, -1, 0xcccccc, 0xcccccc);
            
            c.setClipping();

            // Add a title to the chart
            c.addTitle("Sensor position", "Times New Roman Bold Italic", 15
                ).setBackground(0xdddddd, 0x000000, Chart.glassEffect());

            // Add a legend box at the top of the plot area with 9pts Arial Bold font. We set the 
            // legend box to the same width as the plot area and use grid layout (as opposed to 
            // flow or top/down layout). This distributes the 3 legend icons evenly on top of the 
            // plot area.
            LegendBox b = c.addLegend2( (c.getWidth()-130), 60, 1, "Arial Bold", 9);
            
            b.setBackground(Chart.Transparent, Chart.Transparent);
            b.setWidth((int)(0.7 * TrakstarUIWindow.ActualWidth));

            // Configure the y-axis with a 10pts Arial Bold axis title
            c.yAxis().setTitle("Position (Inches)", "Arial Bold", 10);

            // Scale Y axis from minimum sensor position to maximum position (0 inch - 35 inch)
            c.yAxis().setDateScale(0, 40, 5);

            // Configure the x-axis to auto-scale with at least 75 pixels between major tick and 15 
            // pixels between minor ticks. This shows more minor grid lines on the chart.
            c.xAxis().setTickDensity(75, 15);
          
            // Set the axes width to 2 pixels
            c.xAxis().setWidth(2);
            c.yAxis().setWidth(2);        

            // Now we add the data to the chart
            DateTime lastTime = timeStamps[timeStamps.Length - 1];
            if (lastTime != DateTime.MinValue)
            {
                // Set up the x-axis scale. In this demo, we set the x-axis to show the last 240 
                // samples, with 250ms per sample.
                c.xAxis().setDateScale(lastTime.AddSeconds(
                    -dataRateTimer.Interval.TotalSeconds * timeStamps.Length), lastTime);

                // Set the x-axis label format
                c.xAxis().setLabelFormat("{value|hh:nn:ss}");
                
                // Create a line layer to plot the lines
                LineLayer layer = c.addLineLayer2();

                // The x-coordinates are the timeStamps.
                layer.setXData(timeStamps);

                // Set line thickness
                layer.setLineWidth(8);

                foreach (var sensorData in dataBufferList)
                {
                    if (sensorData.coordinate < 1) // Just plot the x,y,z of each sensor
                    layer.addDataSet(sensorData.buffer, -1, "sensor #: " + (sensorData.id+1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.buffer[sensorData.buffer.Length - 1], " {value|2} "));
                    
                }
            }

            // Assign the chart to the WinChartViewer
            viewer.Chart = c;
        }

        //
        // Utility to shift a DataTime value into an array
        //
        private void shiftData<T>(T[] data, T newValue)
        {
            for (int i = 1; i < data.Length; ++i)
                data[i - 1] = data[i];
            data[data.Length - 1] = newValue;
        }

        public struct SensorBuffer
        {
            public int id;

            public int coordinate;

            public double[] buffer;
        }
    }
}