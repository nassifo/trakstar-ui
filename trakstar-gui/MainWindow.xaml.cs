﻿using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using ChartDirector;
using TrakstarInterface;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;

namespace TrakstarGUI
{
    public partial class TrakstarWindow : Window
    {
        private const int bufferSize = 500;
        private DateTime[] timeStamps = new DateTime[bufferSize];

        private List<SensorBuffer> dataBufferList = new List<SensorBuffer>();

        // Instance of the Trakstar
        private Trakstar bird;

        // Timer used to updated the chart
        private DispatcherTimer chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Render);
        private TimeSpan dataUpdateInterval;

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token;

        public TrakstarWindow()
        {
            InitializeComponent();   
        }
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bird = new Trakstar();

            LogMessageToWindow("Loading Trakstar system...");
            // Initialize Trakstar system
            try
            {
                await bird.InitSystem();
            }
            catch (Exception ex)
            {
                LogMessageToWindow(ex.ToString()); return;
            }

            LogMessageToWindow("Trakstar loaded successfully! Press Run to start recording.");

            // Chart update rate, which can be different from the data generation rate.
            chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(samplePeriod.Text));
            chartUpdateTimer.Tick += chartUpdateTimer_Tick;

            dataUpdateInterval = new TimeSpan(0, 0, 0, 0, bird.GetSamplingRate());

            // Initialize data buffer to no data.
            for (int i = 0; i < timeStamps.Length; ++i)
                timeStamps[i] = DateTime.MinValue;

            // Initialize buffer list
            for (int i = 0; i < bird.GetNumberOfSensors(); i++)
            {
                SensorBuffer sensorBuffer = new SensorBuffer();

                sensorBuffer.id = i;

                sensorBuffer.xBuffer = new double[bufferSize];
                sensorBuffer.yBuffer = new double[bufferSize];
                sensorBuffer.zBuffer = new double[bufferSize];

                dataBufferList.Add(sensorBuffer);
            }

            chartUpdateTimer.Start();

            token = cancellationTokenSource.Token;
            
            var listener = Task.Factory.StartNew( () =>
            {
                FileStream s = new FileStream("records.csv", FileMode.Create, FileAccess.Write,
                          FileShare.None, 4096,
                          FileOptions.Asynchronous | FileOptions.SequentialScan);

                StreamWriter outputFile = new StreamWriter(s);

                while (true)
                {
                    DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records;
                    
                    try
                    {
                        // Get all the new data records for all sensors and all degrees of freedom (x,y,z,a,e,o)
                        records = bird.FetchData();
                    }
                    catch (Exception ex)
                    {
                        LogMessageToWindow(ex.ToString()); continue;
                    }

                    // Write data to file
                    for (int i = 0; i < records.Length; i++)
                    {
                        outputFile.Write(i + ", " + records[i].x + ", " + records[i].y + ", " + records[i].z + ", " + records[i].time + ", ");
                    }

                    outputFile.WriteLine();

                    foreach (var dataBuffer in dataBufferList)
                    {
                        shiftData(dataBuffer.xBuffer, records[dataBuffer.id].x);
                        shiftData(dataBuffer.yBuffer, records[dataBuffer.id].y);
                        shiftData(dataBuffer.zBuffer, records[dataBuffer.id].z);
                    }

                    shiftData(timeStamps, DateTime.Now); 
                    
                    if (token.IsCancellationRequested) break;
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bird != null)
            {
                cancellationTokenSource.Cancel();
                bird.TrakstarOff();
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
            if (bird.IsActive()) 
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
            c.yAxis().setDateScale(-35, 35, 5);

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
                // Set up the x-axis scale
                c.xAxis().setDateScale(lastTime.AddSeconds(
                    -(dataUpdateInterval.TotalSeconds) * timeStamps.Length), lastTime);
                   
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
                    if (sensorData.id == 0)
                    {
                        layer.addDataSet(sensorData.xBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.xBuffer[sensorData.xBuffer.Length - 1], " {value|2} "));
                        layer.addDataSet(sensorData.yBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.yBuffer[sensorData.yBuffer.Length - 1], " {value|2} "));
                        layer.addDataSet(sensorData.zBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.zBuffer[sensorData.zBuffer.Length - 1], " {value|2} "));
                    }
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
            /*   
               for (int i = 1; i < data.Length; ++i)
                   data[i - 1] = data[i];
               data[data.Length - 1] = newValue;
               */

            Array.Copy(data, 1, data, 0, data.Length - 1);

            data[data.Length - 1] = newValue;
        }

        public struct SensorBuffer
        {
            public int id;

            public double[] xBuffer, yBuffer, zBuffer;
        }

        public void LogMessageToWindow(string text)
        {
            LogWindow.AppendText(text);
            LogWindow.AppendText("\u2028"); // Linebreak, not paragraph break
            LogWindow.ScrollToEnd();
        }

        private void Edit_Info_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Created by Omar Nassif. Test message.", "Info Box");
        }

        private void CSVSaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}