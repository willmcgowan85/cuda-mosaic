using ColorMine.ColorSpaces;
using Colourful;
using Colourful.Conversion;
using System.Drawing;

namespace MosaicBuilder
{
    class Pixel
    {
        public byte a, b, c;

        public Pixel(Color color)
        {
            a = color.R;
            b = color.G;
            c = color.B;
        }

        public Pixel(double _a, double _b, double _c)
        {
            a = (byte)_a;
            b = (byte)_b;
            c = (byte)_c;
        }

        public int AsInt(ColorSpace cs)
        {
            RGBColor rgb = new RGBColor(a / 256d, b / 256d, c / 256d);
            switch (cs)
            {
                case ColorSpace.LAB:
                    var lab = new ColourfulConverter().ToLab(rgb);
                    return ((byte)(lab.L * 2.56) << 16) + ((byte)(lab.a + 127) << 8) + (byte)(lab.b + 127);
                case ColorSpace.LUV:
                    var luv = new ColourfulConverter().ToLuv(rgb);
                    return ((byte)(luv.L * 2.56) << 16) + ((byte)(luv.u + 127) << 8) + (byte)(luv.v + 127);
                case ColorSpace.LMS:
                    var lms = new ColourfulConverter().ToLMS(rgb);
                    return ((byte)(lms.L * 256) << 16) + ((byte)(lms.M * 256) << 8) + (byte)(lms.S * 256);
                case ColorSpace.XYY:
                    var xyy = new ColourfulConverter().ToxyY(rgb);
                    return ((byte)(xyy.x * 256) << 16) + ((byte)(xyy.y * 256) << 8) + (byte)(xyy.Luminance * 256);
                case ColorSpace.XYZ:
                    var xyz = new ColourfulConverter().ToXYZ(rgb);
                    return ((byte)(xyz.X * 256) << 16) + ((byte)(xyz.Y * 256) << 8) + (byte)(xyz.Z * 256);
                case ColorSpace.CMY:
                    var cmy = new Rgb(a, b, c).To<Cmy>();
                    return ((byte)(cmy.C * 256) << 16) + ((byte)(cmy.M * 256) << 8) + (byte)(cmy.Y * 256);
                case ColorSpace.HSL:
                    var hsl = new Rgb(a, b, c).To<Hsl>();
                    return ((byte)(hsl.H) << 16) + ((byte)(hsl.S * 2.56) << 8) + (byte)(hsl.L * 2.56);
                case ColorSpace.HSV:
                    var hsv = new Rgb(a, b, c).To<Hsv>();
                    return ((byte)(hsv.H) << 16) + ((byte)(hsv.S * 256) << 8) + (byte)(hsv.V * 256);

                default:
                    return (a << 16) + (b << 8) + c;
            }
        }
    }

    enum ColorSpace
    {
        CMY,
        CMYK,
        HSB,
        HSL,
        HSV,
        LAB,
        LCH,
        LMS,
        LUV,
        RGB,
        XYY,
        XYZ,
        YXY,
    }
}
