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
using Clustering;

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

        private int SkippedFrames = 0;
        private const int FRAMES_TO_SKIP = 1;
        private ColorClusterCreator ct;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Bitmap test = new Bitmap("test2.png");
            //ct = new ColorClusterCreator(test.Width, test.Height);

            //Stopwatch w = new Stopwatch();
            //w.Start();

            //for (int i = 0; i < 1; i++)
            //{
            //    ct.UpdateClusters(test);
            //    ct.BitmapFromClusterMap();
            //}

            //w.Stop();
            //ct.BitmapFromClusterMap().Save("testResult.png");
            //MessageBox.Show(w.ElapsedMilliseconds.ToString());


            FilterInfoCollection videoSources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoSources == null)
            {
                MessageBox.Show("No webcam found.");
                return;
            }

            //assume for now that the first source is the webcam
            videoSource = new VideoCaptureDevice(videoSources[0].MonikerString);

            //if it isn't a webcam, then it can't provide video
            if (videoSource.VideoCapabilities.Length == 0)
            {
                MessageBox.Show("No webcam found.");
                return;
            }

            //select highest resolution with atleast 10 fps.
            //it's a random balance between resolution and fps.
            VideoCapabilities videoInput = videoSource.VideoCapabilities.OrderBy(x => x.FrameSize.Height * x.FrameSize.Width)
                                                                       //.Where(x => x.AverageFrameRate >= 10)
                                                                       .Where(x => x.BitCount == 24)
                                                                       .LastOrDefault();
            if (videoInput == default(VideoCapabilities))
            {
                MessageBox.Show("Webcam doesn't support the required image format.");
                return;
            }
            videoSource.VideoResolution = videoInput;

            ct = new ColorClusterCreator(videoSource.VideoResolution.FrameSize.Width, videoSource.VideoResolution.FrameSize.Height);

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);
            videoSource.Start();

            infoWindow.Show();
            infoWindow.AllowedDistanceSlider.ValueChanged += (se, ev) => ct.SetColorDistance((float)ev.NewValue / 100.0f);
            infoWindow.ClusterView.SelectionChanged += (se, ev) => ct.SetClusterViewType((ClusterViewTypes)((ComboBox)se).SelectedIndex);
            infoWindow.Closed += (se, ev) => this.Close();
        }

        private void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if (SkippedFrames < FRAMES_TO_SKIP)
            {
                SkippedFrames++;
                return;
            }
            else
            {
                SkippedFrames = 0;
            }

            //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
            Bitmap TempBitmap = (Bitmap)eventArgs.Frame.Clone();
            //you'll probably get access violation exceptions
            watch.Reset();
            watch.Start();
                
            TempBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

            try
            {
                ct.UpdateClusters(TempBitmap);
                Bitmap withClusters = ct.BitmapFromClusterMap();

                List<ColorCluster> sortedByRed = ct.GetClustersSortedByMostRed();
                System.Drawing.Point redPoint = sortedByRed.First().CenterPoint;

                List<ColorCluster> sortedByGreen = ct.GetClustersSortedByMostGreen();
                System.Drawing.Point greenPoint = sortedByGreen.First().CenterPoint;

                List<ColorCluster> sortedByBlue = ct.GetClustersSortedByMostBlue();
                System.Drawing.Point bluePoint = sortedByBlue.First().CenterPoint;

                List<ColorCluster> sortedByBlack = ct.GetClustersSortedByMostBlack();
                System.Drawing.Point blackPoint = sortedByBlack.First().CenterPoint;

                using (Graphics g = Graphics.FromImage(withClusters))
                {
                    g.FillEllipse(System.Drawing.Brushes.DarkRed, redPoint.X - 5, redPoint.Y - 5, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.DarkGreen, greenPoint.X - 5, greenPoint.Y - 5, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.DarkBlue, bluePoint.X - 5, bluePoint.Y - 5, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.White, blackPoint.X - 5, blackPoint.Y - 5, 10, 10);
                }

                Dispatcher.Invoke(() => infoWindow.ImageViewer.Source = Convert(withClusters));
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace));
            }

            watch.Stop();
            Dispatcher.Invoke(() => infoWindow.ClusterCreationTime.Text = "Time: " + watch.ElapsedMilliseconds);
            Dispatcher.Invoke(() => infoWindow.ClusterCount.Text = "Cluster Count: " + ct.clusters.Count);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Stop and free the webcam object if application is closing
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }
    }
}
