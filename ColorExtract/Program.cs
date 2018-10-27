using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace ColorExtract
{
    class Program
    {
        static Color Compress(Color pixel, int bitsPerChannel)
        {
            // 2 bits per canale: 4^3 = 64 colori
            // 3 bits per canale: 8^3 = 512 colori

            var scale = Math.Pow(2, bitsPerChannel);
            var r = (byte)((Math.Ceiling((pixel.R / (double)255) * scale) * 255) / scale);
            var g = (byte)((Math.Ceiling((pixel.G / (double)255) * scale) * 255) / scale);
            var b = (byte)((Math.Ceiling((pixel.B / (double)255) * scale) * 255) / scale);
            return Color.FromArgb(r, g, b);
        }

        static Color AverageColor(IEnumerable<Color> colors)
        {
            double count = colors.Count();
            int r = 0, g = 0, b = 0;
            foreach (var color in colors)
            {
                r += color.R;
                g += color.G;
                b += color.B;
            }
            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }

        public static double Euclidean(Color x, Color y)
        {
            return Math.Sqrt(Math.Pow(x.R - y.R, 2) + Math.Pow(x.G - y.G, 2) + Math.Pow(x.B - y.B, 2));
        }

        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Colors.Initialize();

            const bool isBackgroundPredominant = true;

            var bitmap = new Bitmap("C:\\Users\\Federico\\Desktop\\colorextractor.jpg");
            var compressed = new Bitmap(bitmap.Width, bitmap.Height);

            Dictionary<Color, int> histogram = new Dictionary<Color, int>();

            // Creo istogramma pixel normalizzati 

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    var p = Compress(pixel, bitsPerChannel: 3);
                    compressed.SetPixel(x, y, p);

                    if (!histogram.ContainsKey(p))
                        histogram[p] = 1;
                    else
                        histogram[p]++;
                }
            }
            compressed.Save("C:\\Users\\Federico\\Desktop\\colorextractor-compressed.jpg");

            // Calcolo picchi

            double pixelsCount = bitmap.Width * bitmap.Height;
            double averageFrequency = pixelsCount / histogram.Count;
            double averageRelativeFrequency = averageFrequency / pixelsCount;

            Console.WriteLine("Soglia: {0}%", (int)(averageRelativeFrequency * 100));
            Console.WriteLine();

            Dictionary<Color, int> Peaks = new Dictionary<Color, int>();
            double maxRelativeFrequency = 0;
            Color mostFrequentColor = new Color();

            foreach (var frequency in histogram)
            {
                double relativeFrequency = frequency.Value / pixelsCount;

                if (relativeFrequency >= averageRelativeFrequency)
                {
                    Peaks.Add(frequency.Key, frequency.Value);
                    Console.WriteLine($"peak: {frequency.Key.ToString()}: Frequenza Relativa {(int)(relativeFrequency * 100)}%");

                    if (relativeFrequency > maxRelativeFrequency)
                    {
                        maxRelativeFrequency = relativeFrequency;
                        mostFrequentColor = frequency.Key;
                    }
                }
            }
            // Supponendo che il colore più frequente sia lo sfondo, lo rimuovo
            if (isBackgroundPredominant)
                Peaks.Remove(mostFrequentColor);

            // Clustering molto naive via partizionamento 512 -> 64 o 8 colori

            Dictionary<Color, List<Color>> clusters = new Dictionary<Color, List<Color>>();
            foreach (var peak in Peaks)
            {
                var cluster = Compress(peak.Key, bitsPerChannel: 1); // 8: r g b c m y w k

                if (!clusters.ContainsKey(cluster))
                    clusters.Add(cluster, new List<Color> { peak.Key });
                else
                    clusters[cluster].Add(peak.Key);
            }
            List<Color> finalColors = new List<Color>();
            foreach (var cluster in clusters)
                finalColors.Add(AverageColor(cluster.Value));

            // Calcolo nomi https://en.wikipedia.org/wiki/X11_color_names
            Console.WriteLine();

            foreach (var color in finalColors)
            {
                string nearestName = Colors.Names[Colors.Nearest(color)];
                Console.WriteLine(color.ToString() + nearestName);
            }

            Console.WriteLine($"done in {timer.ElapsedMilliseconds} ms");
            Console.Read();
        }
    }

    static class Colors {

        public static void Initialize()
        {
            foreach (var line in Raw.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var l = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var color = ColorTranslator.FromHtml(l[1]);
                if (!Names.ContainsKey(color))
                    Names.Add(color, l[0]);
                else
                    Names[color] += " aka " + l[0];
            }
        }

        public static Color Nearest(Color c)
        {
            var minDistance = double.MaxValue;
            var nearest = new Color();
            foreach (var color in Names.Keys)
            {
                var d = Program.Euclidean(color, c);
                if (d < minDistance)
                {
                    minDistance = d;
                    nearest = color;
                }
            }
            return nearest;
        }

        public static Dictionary<Color, string> Names = new Dictionary<Color, string>();

        public static string Raw = @"
LightPink	#FFB6C1
Pink	#FFC0CB
Crimson	#DC143C
LavenderBlush	#FFF0F5
PaleVioletRed	#DB7093
HotPink	#FF69B4
DeepPink	#FF1493
MediumVioletRed	#C71585
Orchid	#DA70D6
Thistle	#D8BFD8
Plum	#DDA0DD
Violet	#EE82EE
Magenta	#FF00FF
Fuchsia	#FF00FF
DarkMagenta	#8B008B
Purple	#800080
MediumOrchid	#BA55D3
DarkViolet	#9400D3
DarkOrchid	#9932CC
Indigo	#4B0082
BlueViolet	#8A2BE2
MediumPurple	#9370DB
MediumSlateBlue	#7B68EE
SlateBlue	#6A5ACD
DarkSlateBlue	#483D8B
Lavender	#E6E6FA
GhostWhite	#F8F8FF
Blue	#0000FF
MediumBlue	#0000CD
MidnightBlue	#191970
DarkBlue	#00008B
Navy	#000080
RoyalBlue	#4169E1
CornflowerBlue	#6495ED
LightSteelBlue	#B0C4DE
LightSlateGray	#778899
SlateGray	#708090
DodgerBlue	#1E90FF
AliceBlue	#F0F8FF
SteelBlue	#4682B4
LightSkyBlue	#87CEFA
SkyBlue	#87CEEB
DeepSkyBlue	#00BFFF
LightBlue	#ADD8E6
PowderBlue	#B0E0E6
CadetBlue	#5F9EA0
Azure	#F0FFFF
LightCyan	#E0FFFF
PaleTurquoise	#AFEEEE
Cyan	#00FFFF
Aqua	#00FFFF
DarkTurquoise	#00CED1
DarkSlateGray	#2F4F4F
DarkCyan	#008B8B
Teal	#008080
MediumTurquoise	#48D1CC
LightSeaGreen	#20B2AA
Turquoise	#40E0D0
Aquamarine	#7FFFD4
MediumAquamarine	#66CDAA
MediumSpringGreen	#00FA9A
MintCream	#F5FFFA
SpringGreen	#00FF7F
MediumSeaGreen	#3CB371
SeaGreen	#2E8B57
Honeydew	#F0FFF0
LightGreen	#90EE90
PaleGreen	#98FB98
DarkSeaGreen	#8FBC8F
LimeGreen	#32CD32 
Lime	#00FF00
ForestGreen	#228B22
Green	#008000
DarkGreen	#006400
Chartreuse	#7FFF00
LawnGreen	#7CFC00
GreenYellow	#ADFF2F
DarkOliveGreen	#556B2F
YellowGreen	#9ACD32
OliveDrab	#6B8E23
Beige	#F5F5DC
LightGoldenrodYellow	#FAFAD2
Ivory	#FFFFF0
LightYellow	#FFFFE0
Yellow	#FFFF00
Olive	#808000
DarkKhaki	#BDB76B
LemonChiffon	#FFFACD
PaleGoldenrod	#EEE8AA
Khaki	#F0E68C
Gold	#FFD700
Cornsilk	#FFF8DC
Goldenrod	#DAA520
DarkGoldenrod	#B8860B
FloralWhite	#FFFAF0
OldLace	#FDF5E6
Wheat	#F5DEB3
Moccasin	#FFE4B5
Orange	#FFA500
PapayaWhip	#FFEFD5
BlanchedAlmond	#FFEBCD
NavajoWhite	#FFDEAD
AntiqueWhite	#FAEBD7
Tan	#D2B48C
BurlyWood	#DEB887
Bisque	#FFE4C4
DarkOrange	#FF8C00
Linen	#FAF0E6
Peru	#CD853F
PeachPuff	#FFDAB9
SandyBrown	#F4A460
Chocolate	#D2691E
SaddleBrown	#8B4513
Seashell	#FFF5EE
Sienna	#A0522D
LightSalmon	#FFA07A
Coral	#FF7F50
OrangeRed	#FF4500
DarkSalmon	#E9967A
Tomato	#FF6347
MistyRose	#FFE4E1
Salmon	#FA8072
Snow	#FFFAFA
LightCoral	#F08080
RosyBrown	#BC8F8F
IndianRed	#CD5C5C
Red	#FF0000
Brown	#A52A2A
FireBrick	#B22222
DarkRed	#8B0000
Maroon	#800000
White	#FFFFFF
WhiteSmoke	#F5F5F5
Gainsboro	#DCDCDC
LightGrey	#D3D3D3
Silver	#C0C0C0
DarkGray	#A9A9A9
Gray	#808080
DimGray	#696969
Black	#000000
";

    } }
