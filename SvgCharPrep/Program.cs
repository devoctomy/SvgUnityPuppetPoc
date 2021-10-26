using Newtonsoft.Json.Linq;
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

            var json = new JObject();
            var parts = new JArray();

            var fullCopy = svgDocument.Draw();
            fullCopy.Save($"c:\\Temp\\svg\\Test_fullraster.bmp");

            var layers = svgDocument.Children.Where(x => x.GetType() == typeof(SvgGroup)).ToList();
            layers.Reverse();
            svgDocument.Children.Clear();
            for (int curLayer = 0; curLayer < layers.Count(); curLayer++)
            {
                var layer = layers[curLayer] as SvgGroup;
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
                var croppedPdfPath = $"c:\\Temp\\svg\\Test_croppedraster_{layer.ID}.png";
                cropped.Save(croppedPdfPath, System.Drawing.Imaging.ImageFormat.Png);
                partcopy.Dispose();

                svgDocument.Children.Clear();

                var partJson = new JObject();
                partJson.Add("Name", new JValue(layer.ID));
                partJson.Add("LocalPivotPoint", new JValue($"{pivot.X - layer.Bounds.X},{pivot.Y - layer.Bounds.Y}"));
                partJson.Add("WorldBounds", new JValue($"{layer.Bounds.X},{layer.Bounds.Y},{layer.Bounds.Width},{layer.Bounds.Height}"));
                partJson.Add("ImageBase64Data", new JValue(Convert.ToBase64String(System.IO.File.ReadAllBytes(croppedPdfPath))));
                parts.Add(partJson);
            }

            fullCopy.Dispose();

            json.Add("Parts", parts);
            var jsonString = json.ToString(Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("c:\\Temp\\svg\\metadata.puppet", jsonString);
        }
    }
}
