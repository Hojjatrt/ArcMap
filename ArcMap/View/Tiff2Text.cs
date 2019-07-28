using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BitMiracle.LibTiff.Classic;

namespace ArcMap.View
{
    class Tiff2Text
    {
        public static void ExifMetadata(string tiffAddress, string destination)
        {
            using (Tiff image = Tiff.Open(tiffAddress, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                FieldValue[] exifIFDTag = image.GetField(TiffTag.EXIFIFD);
                if (exifIFDTag == null)
                {
                    MessageBox.Show("Exif metadata not found");
                    return;
                }

                int exifIFDOffset = exifIFDTag[0].ToInt();
                if (!image.ReadEXIFDirectory(exifIFDOffset))
                {
                    MessageBox.Show("Could not read EXIF IFD");
                    return;
                }

                using (StreamWriter writer = new StreamWriter(destination))
                {
                    for (TiffTag tag = TiffTag.EXIF_EXPOSURETIME; tag <= TiffTag.EXIF_IMAGEUNIQUEID; ++tag)
                    {
                        FieldValue[] value = image.GetField(tag);
                        if (value != null)
                        {
                            for (int i = 0; i < value.Length; i++)
                            {
                                writer.WriteLine("{0}", tag.ToString());
                                writer.WriteLine("{0} : {1}", value[i].Value.GetType().ToString(), value[i].ToString());
                            }

                            writer.WriteLine();
                        }
                    }
                }
            }
            Process.Start(destination);
        }

        public static void EnumratesTiffTags(string tiffAddress, string destination)
        {
            using (Tiff image = Tiff.Open(tiffAddress, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                using (StreamWriter writer = new StreamWriter(destination))
                {
                    short numberOfDirectories = image.NumberOfDirectories();
                    for (short d = 0; d < numberOfDirectories; ++d)
                    {
                        if (d != 0)
                            writer.WriteLine("---------------------------------");

                        image.SetDirectory((short)d);

                        writer.WriteLine("Image {0}, page {1} has following tags set:\n", tiffAddress, d);
                        for (ushort t = ushort.MinValue; t < ushort.MaxValue; ++t)
                        {
                            TiffTag tag = (TiffTag)t;
                            FieldValue[] value = image.GetField(tag);
                            if (value != null)
                            {
                                for (int j = 0; j < value.Length; j++)
                                {
                                    writer.WriteLine("{0}", tag.ToString());
                                    writer.WriteLine("{0} : {1}", value[j].Value.GetType().ToString(), value[j].ToString());
                                }

                                writer.WriteLine();
                            }
                        }
                    }
                }
            }

            Process.Start(destination);
        }

        public static void PrintDirectiory(string tiffAddress, string destination)
        {
            using (Tiff image = Tiff.Open(tiffAddress, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                byte[] endOfLine = { (byte)'\r', (byte)'\n' };
                using (FileStream stream = new FileStream(destination, FileMode.Create))
                {
                    do
                    {
                        image.PrintDirectory(stream);

                        stream.Write(endOfLine, 0, endOfLine.Length);

                    } while (image.ReadDirectory());
                }
            }

            Process.Start(destination);
        }

        public static void RgbShow(string tiffAddress, string destination)
        {
            // Open the TIFF image
            using (Tiff image = Tiff.Open(tiffAddress, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                // Find the width and height of the image
                FieldValue[] value = image.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = image.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                int imageSize = height * width;
                int[] raster = new int[imageSize];

                // Read the image into the memory buffer
                if (!image.ReadRGBAImage(width, height, raster))
                {
                    MessageBox.Show("Could not read image");
                    return;
                }
                int c = Tiff.GetA(raster[100]);
                using (Bitmap bmp = new Bitmap(200, 200))
                {
                    for (int i = 0; i < bmp.Width; ++i)
                        for (int j = 0; j < bmp.Height; ++j)
                            bmp.SetPixel(i, j, getSample(i + 330, j + 30, raster, width, height));

                    bmp.Save(destination);
                }

            }

            Process.Start(destination);
        }

        private static Color getSample(int x, int y, int[] raster, int width, int height)
        {
            int offset = (height - y - 1) * width + x;
            int red = Tiff.GetR(raster[offset]);
            int green = Tiff.GetG(raster[offset]);
            int blue = Tiff.GetB(raster[offset]);
            return Color.FromArgb(red, green, blue);
        }
    }
}
