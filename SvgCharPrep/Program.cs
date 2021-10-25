using Svg;
using Svg.Transforms;
using System;
using System.Drawing;
using System.Linq;

namespace SvgCharPrep
{
    class Program
    {
        static void Main(string[] args)
        {
            var svgDocument = SvgDocument.Open(@"D:\Source\Characters\Vector\Test.svg");

            var fullCopy = svgDocument.Draw();
            fullCopy.Save($"c:\\Temp\\svg\\Test_fullraster.bmp");

            var layers = svgDocument.Children.Where(x => x.GetType() == typeof(SvgGroup)).ToList();
            layers.Reverse();
            svgDocument.Children.Clear();
            for (int curLayer = 0; curLayer < layers.Count(); curLayer++)
            {
                var layer = layers[curLayer] as SvgGroup;
                Console.WriteLine($"Checking layer {layer.ID}");

                var pivot = PointF.Empty;
                var pivotPoint = layer.Children.SingleOrDefault(x => !string.IsNullOrEmpty(x.ID) && x.ID.StartsWith("pivot", StringComparison.InvariantCultureIgnoreCase));
                if (pivotPoint != null)
                {
                    var translate = pivotPoint.Transforms[0] as SvgTranslate;
                    if(translate != null && pivotPoint.TryGetAttribute("d", out var location))
                    {
                        var locationParts = location.TrimStart('M').Split(' ');
                        pivot = new PointF(
                            float.Parse(locationParts[0]) + translate.X,
                            float.Parse(locationParts[1]) + translate.Y);
                    }
                }

                svgDocument.Children.Add(layer);
                svgDocument.InvalidateChildPaths();
                svgDocument.Write($"c:\\Temp\\svg\\Test_partisolatevector_{layer.ID}.svg", false);
                var partcopy = svgDocument.Draw();
                if (pivot != PointF.Empty)
                {
                    partcopy.SetPixel((int)pivot.X, (int)pivot.Y, Color.Red);
                }
                using var cropped = new Bitmap(
                    (int)layer.Bounds.Width,
                    (int)layer.Bounds.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var croppedGraphics = Graphics.FromImage(cropped);
                croppedGraphics.DrawImage(
                    partcopy,
                    new RectangleF(0, 0, layer.Bounds.Width, layer.Bounds.Height),
                    layer.Bounds,
                    GraphicsUnit.Pixel);
                cropped.Save($"c:\\Temp\\svg\\Test_croppedraster_{layer.ID}.png", System.Drawing.Imaging.ImageFormat.Png);

                partcopy.Save($"c:\\Temp\\svg\\Test_partisolateraster_{layer.ID}.bmp");
                partcopy.Dispose();
                svgDocument.Children.Clear();
                
                //generate json containing details of each part
                //desired location (translated from centre)
                //pivot point
                //name
            }

            fullCopy.Dispose();
        }
    }
}
