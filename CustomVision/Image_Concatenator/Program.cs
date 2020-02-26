using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Image_Concatenator
{
    class Program
    {
        const int HORIZONTAL_SECTIONS_COUNT = 6;
        const int VERTICAL_SECTIONS_COUNT = 6;

        static void Main(string[] args)
        {
            var images = new List<Image>();

            foreach (var path in Directory.GetFiles(@"../../../Resources"))
            {
                images.Add(Image.FromFile(path));
            }

            var batchSize = VERTICAL_SECTIONS_COUNT * HORIZONTAL_SECTIONS_COUNT;

            for (int i = 0; i < images.Count / batchSize; i++)
            {
                var imagesInBatch = images.GetRange(i * batchSize, batchSize);

                var image = ConcatImagesImages(imagesInBatch, VERTICAL_SECTIONS_COUNT, HORIZONTAL_SECTIONS_COUNT);
                image.Save(@$"../../../ConcatImages/image_0{i}.jpg", ImageFormat.Jpeg);
            }
        }

        public static Image ConcatImagesImages(List<Image> images, int matrixWidth, int matrixHeight)
        {
            Image image = null;
            int imgHeigh = 0;

            for (int i = 0; i < matrixWidth; i++)
            {
                var imagesInRow = images.GetRange(i * matrixHeight, matrixHeight);

                Image outputImage = imagesInRow[0];
                int width = 0;

                for (int j = 1; j < imagesInRow.Count; j++)
                {
                    outputImage = ConcatTwoPictures(outputImage, imagesInRow[j], ref width, MainDirection.Horizontal);
                }

                if (image != null)
                {
                    image = ConcatTwoPictures(image, outputImage, ref imgHeigh, MainDirection.Vertical);
                }
                else
                {
                    image = outputImage;
                }
            }

            return image;
        }

        public static Bitmap ConcatTwoPictures(Image firstImage, Image secondImage, ref int directionLength, MainDirection direction)
        {
            var outputImageWidth = 0;
            var outputImageHeight = 0;

            if(direction == MainDirection.Vertical)
            {
                outputImageWidth = firstImage.Width > secondImage.Width ? firstImage.Width : secondImage.Width;
                outputImageHeight = firstImage.Height + secondImage.Height;
            } 
            else
            {
                outputImageWidth = firstImage.Width + secondImage.Width;
                outputImageHeight = firstImage.Height > secondImage.Height ? firstImage.Height : secondImage.Height;
            }

            Bitmap image = new Bitmap(outputImageWidth, outputImageHeight, PixelFormat.Format32bppArgb);

            Point newImagestartpoint = new Point();

            if (direction == MainDirection.Vertical)
            {
                directionLength += secondImage.Height;
                newImagestartpoint = new Point(0, directionLength);
            }
            else
            {
                directionLength += secondImage.Width;
                newImagestartpoint = new Point(directionLength, 0);
            }

            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawImage(firstImage, new Rectangle(new Point(), firstImage.Size), new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(newImagestartpoint, secondImage.Size), new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
            }

            return image;
        }
    }

    public enum MainDirection
    {
        Horizontal,
        Vertical
    }
}
