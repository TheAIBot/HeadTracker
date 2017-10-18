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
            //Bitmap test = new Bitmap("balls1.png");
            //ct = new ColorClusterCreator(test.Width, test.Height);

            //Stopwatch w = new Stopwatch();
            //w.Start();
            //ct.SetColorDistance(14);
            ////for (int i = 0; i < 1; i++)
            ////{
            //    ct.UpdateClusters(test);
            //    ct.BitmapFromClusterMap();
            ////}

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

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(VideoSource_NewFrame);
            videoSource.Start();

            infoWindow.Show();
            infoWindow.AllowedDistanceSlider.ValueChanged += (se, ev) => ct.SetColorDistance((float)ev.NewValue / 100.0f);
            infoWindow.ClusterView.SelectionChanged += (se, ev) => ct.SetClusterViewType((ClusterViewTypes)((ComboBox)se).SelectedIndex);
            infoWindow.UseNoiseRemoval.Click += (se, ev) => ct.SetUseNoiseRemoval((bool)infoWindow.UseNoiseRemoval.IsChecked);
            infoWindow.UseGausBlur.Click += (se, ev) => ct.SetUseGaussBlur((bool)infoWindow.UseGausBlur.IsChecked);
            infoWindow.Closed += (se, ev) => this.Close();
        }

        /// <summary>
        /// Reference: http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html equation 14
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p0"></param>
        /// <returns></returns>
        private float DistanceFromLineToPoint(System.Drawing.PointF p1, System.Drawing.PointF p2, System.Drawing.PointF p0)
        {
            float p12XDiff = p2.X - p1.X;
            float p12YDiff = p2.Y - p1.Y;

            float p01XDiff = p1.X - p0.X;
            float p01YDiff = p1.Y - p0.Y;
            
            return (float)(Math.Abs(p12XDiff * p01YDiff - p01XDiff * p12YDiff) / Math.Sqrt(p12XDiff * p12XDiff + p12YDiff * p12YDiff));
        }

        private (PointF a, PointF b) RotatePoints90DegAroundOrigo(PointF a, PointF b)
        {
            //center of the two points
            float centerX = (a.X + b.X) / 2;
            float centerY = (a.Y + b.Y) / 2;


            //push points to around origo
            float origoAX = a.X - centerX;
            float origoAY = a.Y - centerY;

            float origoBX = b.X - centerX;
            float origoBY = b.Y - centerY;


            //rotate points 90deg
            float rotatedOrigoAX = origoAY;
            float rotatedOrigoAY = -origoAX;

            float rotatedOrigoBX = origoBY;
            float rotatedOrigoBY = -origoBX;


            //now move back to the center
            float rotatedAX = rotatedOrigoAX + centerX;
            float rotatedAY = rotatedOrigoAY + centerY;

            float rotatedBX = rotatedOrigoBX + centerX;
            float rotatedBY = rotatedOrigoBY + centerY;

            return (new PointF(rotatedAX, rotatedAY), new PointF(rotatedBX, rotatedBY));
        }

        private void VideoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
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
                ShowTracker(withClusters);

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

        private void ShowTracker(Bitmap withClusters)
        {
            int biggetClusterSize = ct.clusters.Max(x => x.ClusterSize);
            ColorCluster backgroundCluster = ct.clusters.Single(x => x.ClusterSize == biggetClusterSize);
            ct.clusters.Remove(backgroundCluster);


            List<ColorCluster> sortedByBlack = ct.GetClustersSortedByMostBlack();
            List<ColorCluster> sortedByWhite = ct.GetClustersSortedByMostWhite();

            (ColorCluster red, ColorCluster green, ColorCluster blue, ColorCluster black, float distance) bestGuess = (null, null, null, null, 1000f);

            foreach (ColorCluster blueCluster in sortedByWhite)
            {
                foreach (ColorCluster greenCluster in sortedByWhite)
                {
                    if (greenCluster == blueCluster)
                    {
                        continue;
                    }

                    var rotatedPoints = RotatePoints90DegAroundOrigo(blueCluster.CenterPoint, greenCluster.CenterPoint);
                    PointF blueRotatedPoint = rotatedPoints.a;
                    PointF greenRotatedPoint = rotatedPoints.b;

                    const float MAX_ACCEPTED_DISTANCE = 5;

                    bool foundPair = false;
                    float averageDistance = 0;
                    ColorCluster chosenRedCluster = null;
                    ColorCluster chosenBlackCluster = null;

                    foreach (ColorCluster redCluster in sortedByBlack)
                    {
                        if (redCluster == blueCluster ||
                            redCluster == greenCluster)
                        {
                            continue;
                        }

                        float redDistance = DistanceFromLineToPoint(blueRotatedPoint, greenRotatedPoint, redCluster.CenterPoint);
                        if (redDistance <= MAX_ACCEPTED_DISTANCE)
                        {
                            foreach (ColorCluster blackCluster in sortedByBlack)
                            {
                                if (blackCluster == blueCluster ||
                                    blackCluster == greenCluster ||
                                    blackCluster == redCluster)
                                {
                                    continue;
                                }

                                float blackDistance = DistanceFromLineToPoint(blueRotatedPoint, greenRotatedPoint, blackCluster.CenterPoint);
                                if (blackDistance <= MAX_ACCEPTED_DISTANCE)
                                {
                                    foundPair = true;
                                    averageDistance = (redDistance + blackDistance) / 2;

                                    float centerX = (blueCluster.CenterPoint.X + greenCluster.CenterPoint.X) / 2;
                                    float centerY = (blueCluster.CenterPoint.Y + greenCluster.CenterPoint.Y) / 2;

                                    float redDistanceX = redCluster.CenterPoint.X - centerX;
                                    float redDistanceY = redCluster.CenterPoint.Y - centerY;
                                    float redDistanceToCenter = (float)Math.Sqrt(redDistanceX * redDistanceX + redDistanceY * redDistanceY);

                                    float blackDistanceX = blackCluster.CenterPoint.X - centerX;
                                    float blackDistanceY = blackCluster.CenterPoint.Y - centerY;
                                    float blackDistanceToCenter = (float)Math.Sqrt(blackDistanceX * blackDistanceX + blackDistanceY * blackDistanceY);

                                    float redBlackDistanceX = redDistanceX + blackDistanceX;
                                    float redBlackDistanceY = redDistanceY + blackDistanceY;
                                    float redBlackDistanceToCenter = (float)Math.Sqrt(redBlackDistanceX * redBlackDistanceX + redBlackDistanceY * redBlackDistanceY);

                                    //make sure both vectors don't point in opposite directions
                                    if (redBlackDistanceToCenter < redDistanceToCenter || 
                                        redBlackDistanceToCenter < blackDistanceToCenter)
                                    {
                                        break;
                                    }

                                    if (blackDistanceToCenter < redDistanceToCenter)
                                    {
                                        chosenRedCluster = redCluster;
                                        chosenBlackCluster = blackCluster;
                                    }
                                    else
                                    {
                                        chosenRedCluster = blackCluster;
                                        chosenBlackCluster = redCluster;
                                    }
                                    goto forLoopsEnd;
                                }
                            }
                        }
                    }

                    forLoopsEnd:
                    if (foundPair)
                    {
                        if (averageDistance < bestGuess.distance)
                        {
                            bestGuess = (chosenRedCluster, greenCluster, blueCluster, chosenBlackCluster, averageDistance);
                            goto end;
                        }
                    }
                }
            }
            end:

            if (bestGuess.red != null)
            {
                using (Graphics g = Graphics.FromImage(withClusters))
                {
                    System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Brushes.White, 10);

                    g.DrawLine(pen, bestGuess.black.CenterPoint, bestGuess.red.CenterPoint);
                    g.DrawLine(pen, bestGuess.black.CenterPoint, bestGuess.green.CenterPoint);
                    g.DrawLine(pen, bestGuess.black.CenterPoint, bestGuess.blue.CenterPoint);
                }
            }

            Dispatcher.Invoke(() => infoWindow.TargetOffset.Text = "Offset: " + bestGuess.distance);
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
