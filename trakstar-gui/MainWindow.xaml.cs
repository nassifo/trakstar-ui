using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using ChartDirector;
using TrakstarInterface;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace TrakstarGUI
{
    public partial class TrakstarWindow : Window
    {
        // Chart related variables
        private const int bufferSize = 50;

        ObservableCollection<Sensor> SensorList = new ObservableCollection<Sensor>();

        private int chartElevationAngle;
        private int chartRotationAngle;

        private int chartXWidth;
        private int chartYDepth;
        private int chartZHeight;

        // Instance of the Trakstar
        private Trakstar bird;

        // Timer used to updated the chart
        private DispatcherTimer chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Render);

        // The timer used to keep track of how long we have been recording for
        private DispatcherTimer RecordingTimer = new DispatcherTimer(DispatcherPriority.Background);
        private DateTime RecordingTimerStart;

        // Token for Task
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token;

        // Properties for file I/O
        private String outputFileName = String.Empty;
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
            // Create instance of Trakstar device with selected sampling rate
            bird = new Trakstar(double.Parse(sampleFrequency.Text));
            
            LogMessageToWindow("Loading Trakstar system...");

            // Initialize Trakstar system
            try
            {
                await bird.InitSystem();
            }
            catch (Exception ex)
            {
                LogMessageToWindow(ex.Message.ToString());
            } 

            // Chart update rate, which can be different from the data generation rate.
            chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            chartUpdateTimer.Tick += chartUpdateTimer_Tick;
            chartUpdateTimer.IsEnabled = true;

            // Definitions for the recording timer
            RecordingTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            RecordingTimer.Tick += RecordingTimer_Tick;

            // Begin chart update timer
            chartUpdateTimer.Start();

            // If we have chart view settings from before, load them in
            string _sensorNamesProperty = trakstar_gui.Properties.Settings.Default.SensorDisplayNames;

            Dictionary<int, string> sensorNames = new Dictionary<int, string>(0);

            if (!String.IsNullOrEmpty(_sensorNamesProperty))
            {
                sensorNames = JsonConvert.DeserializeObject<Dictionary<int, string>>(_sensorNamesProperty);
            }

            ElevationAngleControl.Value = trakstar_gui.Properties.Settings.Default.elevationAngle;
            RotationAngleControl.Value = trakstar_gui.Properties.Settings.Default.rotationAngle;
            XWidthControl.Value = trakstar_gui.Properties.Settings.Default.xwidth;
            YDepthControl.Value = trakstar_gui.Properties.Settings.Default.ydepth;
            ZHeightControl.Value = trakstar_gui.Properties.Settings.Default.zheight;

            token = cancellationTokenSource.Token;

            // If the trakstar is active, load sensor data and begin data polling
            if (bird.IsActive())
            {
                LogMessageToWindow("Device setup successful! To begin recording, select an output file by clicking on Save As and then hit Start Recording");

                // Initialize buffer list to contain sensor data
                for (int i = 0; i < bird.GetNumberOfSensors(); i++)
                {
                    Sensor sensor = new Sensor();

                    sensor.id = i;

                    string _displayName;

                    if (sensorNames.Count > 0)
                    {
                        if (!sensorNames.TryGetValue(sensor.id, out _displayName))
                        {
                            sensor.DisplayName = "Sensor " + (i + 1);
                        }
                        else
                        {
                            sensor.DisplayName = _displayName;
                        }
                    } 
                    else
                    {
                        sensor.DisplayName = "Sensor " + (i + 1);
                    }

                    sensor.xBuffer = new double[bufferSize];
                    sensor.yBuffer = new double[bufferSize];
                    sensor.zBuffer = new double[bufferSize];
                    
                    sensor.PlotToggle = true;

                    SensorList.Add(sensor);
                }

                Resources["SensorList"] = SensorList;
                
                // Enable start button because we can record now
                StartButton.IsEnabled = true;
               
                // Start data polling Task/Thread
                var listener = Task.Factory.StartNew(() => DataPolling()
                , token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }          
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            RecordingTimerControl.Text = (DateTime.Now - RecordingTimerStart).ToString(@"hh\:mm\:ss");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bird.IsActive())
            {
                Dictionary<int,string> sensorNames = new Dictionary<int, string>();

                foreach(var sensor in SensorList)
                {
                    sensorNames.Add(sensor.id, sensor.DisplayName);
                }

                // Store sensor names
                trakstar_gui.Properties.Settings.Default.SensorDisplayNames = JsonConvert.SerializeObject(sensorNames);

                // Store set chart view configuration 
                trakstar_gui.Properties.Settings.Default.elevationAngle = chartElevationAngle;
                trakstar_gui.Properties.Settings.Default.rotationAngle = chartRotationAngle;
                trakstar_gui.Properties.Settings.Default.xwidth = chartXWidth;
                trakstar_gui.Properties.Settings.Default.ydepth = chartYDepth;
                trakstar_gui.Properties.Settings.Default.zheight = chartZHeight;

                trakstar_gui.Properties.Settings.Default.Save();

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
        // The viewPortChanged event handler.
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
            
            c.setPlotRegion(c.getWidth() / 2, c.getHeight() / 2 - 50, chartXWidth, chartYDepth, chartZHeight);
            c.setViewAngle(chartElevationAngle, chartRotationAngle);
            c.addLegend(0, 0);
            
            // Add sensor data into plot
            foreach (var sensorData in SensorList)
            {
                string LegendText = sensorData.DisplayName;

                if (sensorData.currentXCoord > 660 || sensorData.currentXCoord < 200 || sensorData.currentYCoord > 500 || sensorData.currentYCoord < -500 || sensorData.currentZCoord > 20 || sensorData.currentZCoord < -300)
                {
                    LegendText += "<*color=FF0000*> - OUT OF OPTIMAL RANGE";
                }

                if (sensorData.PlotToggle)
                    c.addScatterGroup(sensorData.xBuffer, sensorData.yBuffer, sensorData.zBuffer, LegendText, Chart.CircleShape, 10, -1);             
            }

            // Set the x, y and z axis titles
            c.xAxis().setTitle("X-Axis", "Arial Bold", 12);
            c.yAxis().setTitle("Y-Axis", "Arial Bold", 12);
            c.zAxis().setTitle("Z-Axis", "Arial Bold", 12);

            c.zAxis().setReverse();

            c.xAxis().setLinearScale(200, 660, 20);
            c.yAxis().setLinearScale(-500, 500, 50);
            c.zAxis().setLinearScale(-300, 20, 20);

            
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
        private double MillisecondToHz(double period)
        {
            return (1 / (period / 1000));
        }
        private double HzToMillisecond(double frequency)
        {
            return ((1 / frequency) * 1000);
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
                    var regexItem = new Regex("^[a-zA-Z0-9 ]*$");

                    if (String.IsNullOrEmpty(value) || !regexItem.IsMatch(value))
                    {
                        _displayName = "Sensor " + (id + 1);
                    }
                    else
                    {
                        _displayName = value;
                    }
                }
            }

            public bool PlotToggle { get; set; }

            public double[] xBuffer, yBuffer, zBuffer;

            public double currentXCoord, currentYCoord, currentZCoord;
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
                    LogMessageToWindow(ex.Message.ToString());  continue;
                }

                foreach (var dataBuffer in SensorList)
                {
                    // Store the newest coordinate for chart detection
                    dataBuffer.currentXCoord = records[dataBuffer.id].x;
                    dataBuffer.currentYCoord = records[dataBuffer.id].y;
                    dataBuffer.currentZCoord = records[dataBuffer.id].z;

                    // Shift new data into plot buffer
                    shiftData(dataBuffer.xBuffer, records[dataBuffer.id].x);
                    shiftData(dataBuffer.yBuffer, records[dataBuffer.id].y);
                    shiftData(dataBuffer.zBuffer, records[dataBuffer.id].z);
                }
                

                // Write data to file
                if (writeToOutputFlag)
                {
                    for (int i = 0; i < records.Length; i++)
                    {
                        // Write the time once
                        if (i == 0)
                        {
                            // TODO: Writing 'status' and 'button' from bird class instead of hard coding
                            outputFile.Write(records[i].time + ",S_00000000,B_00000000,"); 
                        }

                        outputFile.Write(records[i].x + "," + records[i].y + "," + records[i].z + "," + records[i].a + "," + records[i].e + "," + records[i].r + "," + records[i].quality + ",");
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
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("The file " + dlg.SafeFileName + " already exists at that location, are you sure you want to overwrite it?"
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
                        return;
                    }
                    catch(PathTooLongException ex)
                    {
                        LogMessageToWindow("The selected directory path is too long.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                        return;
                    }
                    catch(DirectoryNotFoundException ex)
                    {
                        LogMessageToWindow("The selected output directory could not be found.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                        return;
                    }
                    catch(IOException ex)
                    {
                        LogMessageToWindow("A writing error has occurred.");
                        LogMessageToWindow("Exception message: " + ex.Message);
                        return;
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
                String headerLine = "timestamp,status,button,";

                for (int i = 1; i < 9; i++)
                {
                    headerLine += ("x" + i + "," + "y" + i + "," + "z" + i + "," + "a" + i + "," + "e" + i + "," + "r" + i + "," + "q" + i + ",");
                }

                outputFile.WriteLine(headerLine);

                writeToOutputFlag = true;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;

                // Enable Timer/Counter
                RecordingTimerControl.Visibility = System.Windows.Visibility.Visible;
                RecordingTimerStart = DateTime.Now;
                RecordingTimer.Start();
                LogMessageToWindow("Recording session started!");

            } else
            {
                MessageBox.Show("Please select an output file first, by clicking on the Save As button.", "No output file selected");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            LogMessageToWindow("Stopping Recording Session!");
            LogMessageToWindow("Data saved to: " + outputFileName);
           
            writeToOutputFlag = false;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            // Reset Save As parameters and close file stream?
            outputFileName = String.Empty;
            outputFileLabel.Text = "No output file chosen";
            readyToWriteToOutput = false;

            // Reset Timer
            RecordingTimerControl.Visibility = System.Windows.Visibility.Hidden;
            RecordingTimerControl.Text = "";
            RecordingTimer.Stop();

            outputFile.Close();
            s.Close();
        }

        private void powerLineFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedText = (powerLineFrequency.SelectedValue as ComboBoxItem).Content as string;

            if (!string.IsNullOrEmpty(selectedText))
            {
                int error = bird.setPowerLineFrequency(double.Parse(selectedText));

                if (error == -1)
                {
                    LogMessageToWindow("Could not change power line frequency.");
                }
                else
                {
                    LogMessageToWindow("Power line frequency settings changed to: " + selectedText);
                }
            }
        }

        private void sampleFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedText = (sampleFrequency.SelectedValue as ComboBoxItem).Content as string;

            if (!string.IsNullOrEmpty(selectedText))
            {
                chartUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(selectedText));
                
                int error = bird.setSamplingFrequency(double.Parse(selectedText));

                if (error == -1)
                {
                    LogMessageToWindow("Could not change sampling frequency.");
                }
                else
                {
                    LogMessageToWindow("Sampling frequency of Trakstar changed successfully to: " + selectedText);
                }
            }
        }
        #endregion

        #region Chart View Events
        private void ElevationAngleControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!String.IsNullOrEmpty(ElevationAngleControl.Text))
            chartElevationAngle = int.Parse(ElevationAngleControl.Text);
        }

        private void RotationAngleControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!String.IsNullOrEmpty(RotationAngleControl.Text))
            chartRotationAngle = int.Parse(RotationAngleControl.Text);
        }

        private void ZHeightControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!String.IsNullOrEmpty(ZHeightControl.Text))
                chartZHeight = int.Parse(ZHeightControl.Text);
        }

        private void YDepthControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!String.IsNullOrEmpty(YDepthControl.Text))
                chartYDepth = int.Parse(YDepthControl.Text);
        }

        private void XWidthControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!String.IsNullOrEmpty(XWidthControl.Text))
                chartXWidth = int.Parse(XWidthControl.Text);
        }
        #endregion
    }
}