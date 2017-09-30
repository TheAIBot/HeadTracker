using AForge.Video.DirectShow;
using ImageInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeadTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCaptureDevice videoSource;
        private readonly Stopwatch watch = new Stopwatch();
        private readonly InfoWindow infoWindow = new InfoWindow();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Bitmap test = new Bitmap("test2.png");
            ct = new ColorClusterCreator(test.Width, test.Height);

            Stopwatch w = new Stopwatch();
            w.Start();

            for (int i = 0; i < 1; i++)
            {
                ct.UpdateClusters(test);
                ct.BitmapFromClusterMap();
            }

            w.Stop();
            ct.BitmapFromClusterMap().Save("testResult.png");
            MessageBox.Show(w.ElapsedMilliseconds.ToString());


            FilterInfoCollection videoSources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoSources == null)
            {
                MessageBox.Show("No webcam found.");
            }

            //assume for now that the first source is the webcam
            videoSource = new VideoCaptureDevice(videoSources[0].MonikerString);

            //if it isn't a webcam, then it can't provide video
            if (videoSource.VideoCapabilities.Length == 0)
            {
                MessageBox.Show("No webcam found.");
            }

            //select highest resolution with atleast 10 fps.
            //it's a random balance between resolution and fps.
            videoSource.VideoResolution = videoSource.VideoCapabilities.OrderBy(x => x.FrameSize.Height * x.FrameSize.Width)
                                                                       //.Where(x => x.AverageFrameRate >= 10)
                                                                       .Last();

            ct = new ColorClusterCreator(videoSource.VideoResolution.FrameSize.Width, videoSource.VideoResolution.FrameSize.Height);

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);
            videoSource.Start();

            infoWindow.Show();
            infoWindow.AllowedDistanceSlider.ValueChanged += (se, ev) => dd = (float)ev.NewValue / 100.0f;
            infoWindow.Closed += (se, ev) => this.Close();
        }

        float dd = 3;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Stop and free the webcam object if application is closing
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }

        int number = 0;
        ColorClusterCreator ct;

        private void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if (number < 0)
            {
                number++;
                return;
            }
            else
            {
                number = 0;
            }
            try
            {
                //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
                Bitmap TempBitmap = (Bitmap)eventArgs.Frame.Clone();
                //you'll probably get access violation exceptions
                watch.Reset();
                watch.Start();
                
                TempBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                try
                {
                    ct.SetColorDistance(dd);
                    ct.UpdateClusters(TempBitmap);
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(() => MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace));
                }


                Dispatcher.Invoke(() => infoWindow.ImageViewer.Source = Convert(ct.BitmapFromClusterMap()));

                watch.Stop();
                Dispatcher.Invoke(() => infoWindow.ClusterCreationTime.Text = "Time: " + watch.ElapsedMilliseconds);

            }
            catch (Exception e)
            {
                throw;
            }
        }

        public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}
