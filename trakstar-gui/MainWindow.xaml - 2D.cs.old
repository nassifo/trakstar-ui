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
using System.Timers;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace TrakstarGUI
{
    public partial class TrakstarWindow : Window
    {
        private const int bufferSize = 300;
        private DateTime[] timeStamps = new DateTime[bufferSize];

        ObservableCollection<Sensor> SensorList = new ObservableCollection<Sensor>();

        // Instance of the Trakstar
        private Trakstar bird;

        // Timer used to updated the chart
        private DispatcherTimer chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Render);
        private TimeSpan dataUpdateInterval;

        // The timer used to keep track of how long we have been recording for
        private DispatcherTimer RecordingTimer = new DispatcherTimer(DispatcherPriority.Background);
        private DateTime RecordingTimerStart;

        // Token for Task
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token;

        // Properties for file I/O
        private String outputFileName = String.Empty; // Current output file name
        private String prevOutputFileName = String.Empty; // Keep track of the previous output file name
        private bool readyToWriteToOutput = false;
        private bool writeToOutputFlag = false;
        private FileStream s;
        private StreamWriter outputFile;

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
                LogMessageToWindow(ex.Message.ToString()); return;
            }

            LogMessageToWindow("Trakstar loaded successfully! Press Start to start recording.");

            // Chart update rate, which can be different from the data generation rate.
            chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(samplePeriod.Text));
            chartUpdateTimer.Tick += chartUpdateTimer_Tick;
            chartUpdateTimer.IsEnabled = true;

            // For XYChart X-Axis
            dataUpdateInterval = new TimeSpan(0, 0, 0, 0, bird.GetSamplingRate());

            // Definitions for the recording timer
            RecordingTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            RecordingTimer.Tick += RecordingTimer_Tick;

            // Initialize time stamps
            for (int i = 0; i < timeStamps.Length; ++i)
                timeStamps[i] = DateTime.MinValue;

            // Initialize buffer list
            for (int i = 0; i < bird.GetNumberOfSensors(); i++)
            {
                Sensor sensor = new Sensor();

                sensor.id = i;
                sensor.DisplayName = "Sensor " + i;
                sensor.xBuffer = new double[bufferSize];
                sensor.yBuffer = new double[bufferSize];
                sensor.zBuffer = new double[bufferSize];

                sensor.IsSelectedX = false;
                sensor.IsSelectedY = false;
                sensor.IsSelectedZ = false;

                SensorList.Add(sensor);
            }

            Resources["SensorList"] = SensorList;

            chartUpdateTimer.Start();

            token = cancellationTokenSource.Token;

            StartButton.IsEnabled = true;

            // Start data polling Task/Thread
            var listener = Task.Factory.StartNew( () => DataPolling()
            , token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {

            Timer.Text = (DateTime.Now - RecordingTimerStart).ToString(@"hh\:mm\:ss");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bird.IsActive())
            {
                cancellationTokenSource.Cancel();
                bird.TrakstarOff();

                if (readyToWriteToOutput)
                {
                    outputFile.Close();
                    s.Close();
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
            // If the trakstar is connected and active then draw chart
            if (bird.IsActive()) drawChart(sender as WPFChartViewer);
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

            // Add a legend box at the top of the plot area with 9pts Arial Bold font. We set the 
            // legend box to the same width as the plot area and use grid layout (as opposed to 
            // flow or top/down layout). This distributes the 3 legend icons evenly on top of the 
            // plot area.
            LegendBox b = c.addLegend2( (c.getWidth()-130), 60, 1, "Arial Bold", 9);
            
            b.setBackground(Chart.Transparent, Chart.Transparent);
            b.setWidth((int)(0.7 * TrakstarUIWindow.ActualWidth));

            // Configure the y-axis with a 10pts Arial Bold axis title
            c.yAxis().setTitle("Position (Inches)", "Arial Bold", 10);

            // Scale Y axis from minimum sensor position to maximum position (-35 inch to 35 inch)
            c.yAxis().setDateScale(-35, 35, 5);
   
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

                foreach (var sensorData in SensorList)
                {
                    if (sensorData.IsSelectedX)
                    layer.addDataSet(sensorData.xBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.xBuffer[sensorData.xBuffer.Length - 1], " {value|2} "));
                        
                    if (sensorData.IsSelectedY)
                    layer.addDataSet(sensorData.yBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.yBuffer[sensorData.yBuffer.Length - 1], " {value|2} "));

                    if (sensorData.IsSelectedZ)
                    layer.addDataSet(sensorData.zBuffer, -1, "sensor #: " + (sensorData.id + 1) + "<*bgColor=FFCCCC*>" + c.formatValue(sensorData.zBuffer[sensorData.zBuffer.Length - 1], " {value|2} "));
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

        public class Sensor
        {
            public int id;

            private string _displayName;
            public string DisplayName {
                get
                {
                    return _displayName;
                }
                set
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        _displayName = "Sensor " + id.ToString();
                    } else
                    {
                        _displayName = value;
                    }
                }
            }

            public bool IsSelectedX { get; set; }
            public bool IsSelectedY { get; set; }
            public bool IsSelectedZ { get; set; }

            public double[] xBuffer, yBuffer, zBuffer;
        }

        public void LogMessageToWindow(string text)
        {
            LogWindow.AppendText(text);
            LogWindow.AppendText("\u2028"); // Linebreak, not paragraph break
            LogWindow.ScrollToEnd();
        }

        private void DataPolling()
        {
            while (true)
            {
                DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records;

                try
                {
                    // Get all the new data records for all sensors and all degrees of freedom (x,y,z,a,e,r)
                    records = bird.FetchData();
                }
                catch (Exception ex)
                {
                    LogMessageToWindow(ex.Message.ToString()); continue;
                }

                foreach (var dataBuffer in SensorList)
                {
                    shiftData(dataBuffer.xBuffer, records[dataBuffer.id].x);
                    shiftData(dataBuffer.yBuffer, records[dataBuffer.id].y);
                    shiftData(dataBuffer.zBuffer, records[dataBuffer.id].z);
                }

                shiftData(timeStamps, DateTime.Now);

                // Write data to file
                if (writeToOutputFlag)
                {
                    for (int i = 0; i < records.Length; i++)
                    {
                        outputFile.Write(i + ", " + records[i].x + ", " + records[i].y + ", " + records[i].z + ", " + records[i].time + ", ");
                    }

                    outputFile.WriteLine();
                }

                if (token.IsCancellationRequested) break;
            }
        }

        #region Button Click Events
        private void Edit_Info_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Created by Omar Nassif. Test message.", "Info Box");
        }


        private void CSVSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Choose appropriate file name"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "CSV Files (.csv)|*.csv"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                if (File.Exists(dlg.FileName))
                {
                    // If the file already exists, confirm that they want to overwrite it
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("The file " + dlg.SafeFileName + " already exists at that location, do you want to overwrite it?"
                        , "Overwrite Confirmation"
                        , System.Windows.MessageBoxButton.YesNo);

                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        // Save document
                        outputFileName = dlg.FileName;
                        outputFileLabel.Text = "Saving as: " + dlg.SafeFileName;
                        
                    }
                    else
                    {
                        outputFileName = String.Empty;
                        outputFileLabel.Text = "No output file chosen";
                        readyToWriteToOutput = false;
                    }
                }
                else
                {
                    // Save document
                    outputFileName = dlg.FileName;
                    outputFileLabel.Text = "Saving as: " + dlg.SafeFileName;
                }

                // If there is a selected file name that means we can write to that file (or OVERWRITE)
                if (!String.IsNullOrEmpty(outputFileName))
                {
                    // Create file and initialize StreamWriter object
                    try
                    {
                        s = new FileStream(outputFileName, FileMode.Create, FileAccess.Write,
                        FileShare.None, 4096,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogMessageToWindow("You do not have permission to write to: " + outputFileName);
                        LogMessageToWindow("Exception message: " + ex.Message);
                    }
                    catch(PathTooLongException ex)
                    {
                        LogMessageToWindow("The selected directory path is too long.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                    }
                    catch(DirectoryNotFoundException ex)
                    {
                        LogMessageToWindow("The selected output directory could not be found.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                    }
                    catch(IOException ex)
                    {
                        LogMessageToWindow("A writing error has occurred.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                    }
                    finally
                    {
                        if (s == null)
                        {
                            outputFileName = String.Empty;
                            outputFileLabel.Text = "No output file chosen";
                            readyToWriteToOutput = false;
                        }
                    }

                    // Initialize the writing stream
                    try
                    {
                        outputFile = new StreamWriter(s);
                    }
                    catch(ArgumentNullException ex)
                    {
                        LogMessageToWindow("Exception message: " + ex.Message);
                        outputFileName = String.Empty;
                        outputFileLabel.Text = "No output file chosen";
                        readyToWriteToOutput = false;
                        return;
                    }

                    // Flag to indicate we are ready to write to the file
                    readyToWriteToOutput = true;                 
                }
            }
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (readyToWriteToOutput)
            {
                writeToOutputFlag = true;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;

                // Enable Timer/Counter
                Timer.Visibility = System.Windows.Visibility.Visible;
                RecordingTimerStart = DateTime.Now;
                RecordingTimer.Start();

            } else
            {
                MessageBox.Show("Please select an output file first, by clicking on the Save As button.", "No output file selected");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            writeToOutputFlag = false;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            // Reset Save As parameters and close file stream?
            outputFileName = String.Empty;
            outputFileLabel.Text = "No output file chosen";
            readyToWriteToOutput = false;

            // Reset Timer
            Timer.Visibility = System.Windows.Visibility.Hidden;
            Timer.Text = "";
            RecordingTimer.Stop();

            outputFile.Close();
            s.Close();
        }
        #endregion


    }
}