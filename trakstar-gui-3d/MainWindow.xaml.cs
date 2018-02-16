using ChartDirector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TrakstarInterface;

namespace trakstar_gui_3d
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int bufferSize = 300;
        private DateTime[] timeStamps = new DateTime[bufferSize];

        private List<SensorBuffer> dataBufferList = new List<SensorBuffer>();

        // Instance of the Trakstar
        private Trakstar bird;

        // Timer used to updated the chart
        private DispatcherTimer chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Render);
     
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token;

        public MainWindow()
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

            var listener = Task.Factory.StartNew(() =>
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

                    // TODO: CHECK IF ARRAY.COPY IS FASTER AND MAYBE MAKE THIS METHOD ASYNC ALSO?
                    shiftData(timeStamps, DateTime.Now); // Add time stamp to mark the time we retreived the data

                    if (token.IsCancellationRequested)
                        break;
                }

                //cleanup
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
            // Create a ThreeDScatterChart object
            ThreeDScatterChart c = new ThreeDScatterChart((int)(TrakstarUIWindow.ActualWidth - 200), (int)(TrakstarUIWindow.ActualHeight - LogWindow.ActualHeight - 30));

            c.setPlotRegion(c.getWidth()/2, c.getHeight()/2 - 50, 500, 500, 200);
            c.setViewAngle(90);
            
            // Add data into scatter plot
            foreach (var sensorData in dataBufferList)
            {
                if (sensorData.id == 0)
                    c.addScatterGroup(sensorData.xBuffer, sensorData.yBuffer, sensorData.zBuffer, "", Chart.CircleShape, 10, -1);
            }

            // Set the x, y and z axis titles using 10 points Arial Bold font
            c.xAxis().setTitle("X-Axis Place Holder", "Arial Bold", 10);
            c.yAxis().setTitle("Y-Axis Place Holder", "Arial Bold", 10);
            c.zAxis().setTitle("Z-Axis Place Holder", "Arial Bold", 10);

            c.zAxis().setLinearScale(-40, 40, 5);
            c.xAxis().setLinearScale(-40, 40, 5);
            c.yAxis().setLinearScale(-40, 40, 5);

            // Assign the chart to the WinChartViewer
            viewer.Chart = c;
            
        }

        //
        // Utility to shift a DataTime value into an array
        // TODO: LOOK AT USING ARRAY.COPY INSTEAD OF MANUAL SHIFTING
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
            LogWindow.AppendText("\u2028");
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
