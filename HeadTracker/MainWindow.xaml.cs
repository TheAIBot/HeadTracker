using AForge.Video.DirectShow;
using ImageInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            /*
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                colors.Add(System.Drawing.Color.FromKnownColor((KnownColor)colorValue));
            }
            Bitmap test = new Bitmap("test3.png");
            Stopwatch w = new Stopwatch();
            w.Start();
            ColorClusterCreator ct = null;
            //for (int i = 0; i < 5; i++)
            //{
                ct = new ColorClusterCreator(test);
            //}

            w.Stop();
            //MessageBox.Show(w.ElapsedMilliseconds.ToString());
            for (int i = 0; i < ct.clusters.Count; i++)
            {
                ColorCluster cluster = ct.clusters[i];
                System.Drawing.Color color = colors[i % colors.Count];

                foreach (PixelStretch stretch in cluster.PixelStretches)
                {
                    for (int x = stretch.startX; x <= stretch.endX; x++)
                    {
                        test.SetPixel(x, stretch.y, color);
                    }
                }
            }
            
            test.Save("testResult4.png");
            MessageBox.Show(w.ElapsedMilliseconds.ToString());
            */

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
            videoSource.VideoResolution = videoSource.VideoCapabilities.OrderBy(x => x.FrameSize.Height + x.FrameSize.Width)
                                                                       .Where(x => x.AverageFrameRate >= 10)
                                                                       .Last();

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);
            videoSource.Start();

            infoWindow.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Stop and free the webcam object if application is closing
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }

        int number = 0;

        private void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if (number < 5)
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

                ColorClusterCreator ct = new ColorClusterCreator(TempBitmap);
                
                List<ColorCluster> sortedByRed = ct.GetClustersSortedByMostRed();
                System.Drawing.Point redPoint = sortedByRed.First().CenterPoint;

                List<ColorCluster> sortedByGreen = ct.GetClustersSortedByMostGreen();
                System.Drawing.Point greenPoint = sortedByGreen.First().CenterPoint;

                List<ColorCluster> sortedByBlue = ct.GetClustersSortedByMostBlue();
                System.Drawing.Point bluePoint = sortedByBlue.First().CenterPoint;

                List<ColorCluster> sortedByBlack = ct.GetClustersSortedByMostBlack();
                System.Drawing.Point blackPoint = sortedByBlack.First().CenterPoint;

                DrawClusters(TempBitmap, sortedByRed, System.Drawing.Color.Red);
                DrawClusters(TempBitmap, sortedByGreen, System.Drawing.Color.Green);
                DrawClusters(TempBitmap, sortedByBlue, System.Drawing.Color.Blue);
                DrawClusters(TempBitmap, sortedByBlack, System.Drawing.Color.Black);
                
                using (Graphics g = Graphics.FromImage(TempBitmap))
                {
                    g.FillEllipse(System.Drawing.Brushes.DarkRed, redPoint.X, redPoint.Y, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.DarkGreen, greenPoint.X, greenPoint.Y, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.DarkBlue, bluePoint.X, bluePoint.Y, 10, 10);
                    g.FillEllipse(System.Drawing.Brushes.Black, blackPoint.X, blackPoint.Y, 10, 10);
                }
                

                /*
                List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
                foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
                {
                    colors.Add(System.Drawing.Color.FromKnownColor((KnownColor)colorValue));
                }
                for (int i = 0; i < ct.clusters.Count; i++)
                {
                    ColorCluster cluster = ct.clusters[i];
                    System.Drawing.Color color = colors[i % colors.Count];

                    foreach (PixelStretch stretch in cluster.PixelStretches)
                    {
                        for (int x = stretch.startX; x <= stretch.endX; x++)
                        {
                            TempBitmap.SetPixel(x, stretch.y, color);
                        }
                    }
                }
                */

                Dispatcher.Invoke(() => infoWindow.ImageViewer.Source = Convert(TempBitmap));

                watch.Stop();
                string fisk = (watch.ElapsedMilliseconds == 0) ? "30" : (1000 / watch.ElapsedMilliseconds).ToString();
                //this.Invoke(new Action(() => label1.Text = fisk));

            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void DrawClusters(Bitmap image, List<ColorCluster> clusters, System.Drawing.Color color)
        {
            for (int i = 0; i < 2; i++)
            {
                foreach (PixelStretch stretch in clusters[i].PixelStretches)
                {
                    for (int x = stretch.startX; x <= stretch.endX; x++)
                    {
                        image.SetPixel(x, stretch.y, color);
                    }
                }
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
