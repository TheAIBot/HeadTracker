using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                colors.Add(System.Drawing.Color.FromKnownColor((KnownColor)colorValue));
            }
            Bitmap test = new Bitmap("test3.png");
            Stopwatch w = new Stopwatch();
            w.Start();
            ColorClusterCreator ct = new ColorClusterCreator(test);
            w.Stop();
            MessageBox.Show(w.ElapsedMilliseconds.ToString());
            
            for (int i = 0; i < ct.clusters.Count; i++)
            {
                ColorCluster cluster = ct.clusters[i];
                if (cluster.ClusterSize <= 10)
                {
                    continue;
                }
                System.Drawing.Color color = colors[i % colors.Count];

                foreach (List<PixelStretch> listStretches in cluster.PixelStretches)
                {
                    foreach (PixelStretch stretch in listStretches)
                    {
                        for (int x = stretch.startX; x <= stretch.endX; x++)
                        {
                            test.SetPixel(x, stretch.y, color);
                        }
                    }
                }
            }
            
            test.Save("testResult3.png");
            

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

        private void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
                //you'll probably get access violation exceptions
                watch.Reset();
                watch.Start();
                Bitmap TempBitmap = (Bitmap)eventArgs.Frame.Clone();
                TempBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                /*
                ColorClusterCreator blackClusters  = new ColorClusterCreator(TempBitmap, 0  , 0  , 0);
                ColorClusterCreator redClusters    = new ColorClusterCreator(TempBitmap, 255, 0  , 0);
                ColorClusterCreator greenClusters  = new ColorClusterCreator(TempBitmap, 0  , 255, 0);
                ColorClusterCreator blueClusters   = new ColorClusterCreator(TempBitmap, 0  , 0  , 255);
                ColorClusterCreator orangeClusters = new ColorClusterCreator(TempBitmap, 255, 127, 0);

                try
                {
                    using (Graphics g = Graphics.FromImage(TempBitmap))
                    {
                        System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Cyan, 10);
                        g.DrawEllipse(pen, blackClusters.clusters.First().CenterPoint.X, blackClusters.clusters.First().CenterPoint.Y, 10, 10);
                    }
                }
                catch (Exception e)
                {
                    throw;
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
