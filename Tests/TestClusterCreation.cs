using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

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
        }

        [TestMethod]
        public void Vertical_Clusters()
        {

        }

        [TestMethod]
        public void Horizontal_Colored_Clusters()
        {

        }

        [TestMethod]
        public void U_Merge()
        {

        }

        [TestMethod]
        public void Left_Under_Merge()
        {

        }

        [TestMethod]
        public void Right_Under_Merge()
        {

        }

        [TestMethod]
        public void Spiral()
        {

        }

        [TestMethod]
        public void ZigZag()
        {

        }

        private void CompateClusterMaps(Bitmap image, int[] expectedClusterMap)
        {
            ColorClusterCreator fisk;
        }
    }
}
