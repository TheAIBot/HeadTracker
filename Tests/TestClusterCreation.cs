using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using Clustering;
using System.Text;
using System.IO;

namespace Tests
{
    [TestClass]
    public class TestClusterCreation
    {
        [TestMethod]
        public void Horizontal_Clusters()
        {
            Bitmap image = new Bitmap("Images/Horizontal_Lines.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 2, 2, 2, 2, 2, 2, 2, 2, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 3, 3, 3, 3, 3, 3, 3, 3, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 4, 4, 4, 4, 4, 4, 4, 4, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Vertical_Clusters()
        {
            Bitmap image = new Bitmap("Images/Vertical_Lines.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 1, 0, 2, 0, 3, 0, 4, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Horizontal_Colored_Clusters()
        {
            Bitmap image = new Bitmap("Images/Horizontal_Colored_Stripes.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void U_Merge()
        {
            Bitmap image = new Bitmap("Images/U_Merge.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 1, 0, 3, 0, 3, 0, 0,
                0, 1, 0, 1, 0, 3, 0, 3, 0, 0,
                0, 1, 1, 1, 0, 3, 3, 3, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 5, 0, 5, 0, 7, 0, 7, 0, 0,
                0, 5, 0, 5, 0, 7, 0, 7, 0, 0,
                0, 5, 5, 5, 0, 7, 7, 7, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Left_Under_Merge()
        {
            Bitmap image = new Bitmap("Images/Left_Under_Merge.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 0, 0, 2, 3, 0, 0, 0,
                0, 0, 1, 0, 0, 2, 3, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Right_Under_Merge()
        {
            Bitmap image = new Bitmap("Images/Right_Under_Merge.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 2, 3, 0, 0, 1, 0, 0, 0,
                0, 0, 2, 3, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Spiral()
        {
            Bitmap image = new Bitmap("Images/Spiral.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 1, 0,
                0, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 0, 0, 0, 0, 1, 0, 1, 0,
                0, 1, 0, 1, 0, 0, 1, 0, 1, 0,
                0, 1, 0, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 0, 0, 0, 0, 0, 0, 1, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void ZigZag()
        {
            Bitmap image = new Bitmap("Images/ZigZag.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 0, 0, 2, 0, 0, 0, 0,
                0, 1, 0, 0, 2, 2, 0, 0, 5, 0,
                0, 0, 0, 2, 2, 0, 0, 5, 5, 0,
                0, 0, 2, 2, 0, 0, 5, 5, 0, 0,
                0, 2, 2, 0, 0, 5, 5, 0, 0, 0,
                0, 2, 0, 0, 5, 5, 0, 0,20, 0,
                0, 0, 0, 5, 5, 0, 0,20,20, 0,
                0, 0, 5, 5, 0, 0,20,20, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        [TestMethod]
        public void Spagetti()
        {
            Bitmap image = new Bitmap("Images/Spagetti.png");
            int[] exptectedClusterMap = new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 1, 1, 1, 1, 0, 0,
                0, 1, 0, 1, 1, 0, 0, 1, 1, 0,
                0, 1, 1, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 1, 0, 0,
                0, 1, 1, 1, 1, 1, 0, 1, 0, 0,
                0, 1, 0, 0, 0, 0, 0, 1, 1, 0,
                0, 1, 0, 1, 0, 0, 1, 1, 0, 0,
                0, 1, 0, 1, 1, 0, 0, 1, 1, 0,
                0, 1, 1, 1, 0, 0, 0, 0, 0, 0
            };

            CompateClusterMaps(image, exptectedClusterMap);
        }

        private void CompateClusterMaps(Bitmap image, int[] expectedClusterMap)
        {
            ColorClusterCreator clusterCreator = new ColorClusterCreator(image.Width, image.Height);
            clusterCreator.SetUseNoiseRemoval(false);
            clusterCreator.UpdateClusters(image);

            int[] actualClusterMap = clusterCreator.GetClusterMap();

            Assert.AreEqual(expectedClusterMap.Length, actualClusterMap.Length);
            for (int i = 0; i < expectedClusterMap.Length; i++)
            {
                Assert.AreEqual(expectedClusterMap[i], actualClusterMap[i], "Maps doesn't match." + Environment.NewLine + 
                                                                            "Expected: " + Environment.NewLine + ClusterMapAsString(expectedClusterMap, image.Width, image.Height) + Environment.NewLine + 
                                                                            "Actual: " + Environment.NewLine + ClusterMapAsString(actualClusterMap, image.Width, image.Height));
            }
        }

        private string ClusterMapAsString(int[] map, int width, int height)
        {
            StringBuilder sBuilder = new StringBuilder();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    string toFill = "000";
                    string number = map[y * width + x].ToString();
                    string result = toFill.Substring(0, toFill.Length - number.Length) + number;
                    sBuilder.Append(result);
                    sBuilder.Append(" ");
                }
                sBuilder.Append(Environment.NewLine);
            }

            return sBuilder.ToString();
        }
    }
}
