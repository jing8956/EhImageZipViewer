// From: https://android.googlesource.com/platform/tools/base
// Commit: 6532be0a3cd09fb277304b19c00fe14c4f26d12b
// Path: sdk-common/src/main/java/com/android/ide/common/vectordrawable/Svg2Vector.java

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Android.OS;

namespace Jing8956.Maui.SvgImageSource.VectorDrawable;

/// <summary>Converts SVG to VectorDrawable's XML.</summary>
/// <remarks>
/// There are two major functions:<br/>
/// - {@link #parse} Parses the .svg file, builds and optimizes an internal tree<br/>
/// - {@link #writeFile} Traverses the internal tree and produces XML output
/// </remarks>
internal partial class Svg2Vector
{
    // private static final Logger logger = Logger.getLogger(Svg2Vector.class.getSimpleName());
    private const string SVG_DEFS = "defs";
    private const string SVG_USE = "use";
    private const string SVG_HREF = "href";
    private const string SVG_XLINK_HREF = "xlink:href";

    public const string SVG_POLYGON = "polygon";
    public const string SVG_POLYLINE = "polyline";
    public const string SVG_RECT = "rect";
    public const string SVG_CIRCLE = "circle";
    public const string SVG_LINE = "line";
    public const string SVG_PATH = "path";
    public const string SVG_ELLIPSE = "ellipse";
    public const string SVG_GROUP = "g";
    public const string SVG_STYLE = "style";
    public const string SVG_DISPLAY = "display";
    public const string SVG_CLIP_PATH_ELEMENT = "clipPath";

    public const string SVG_D = "d";
    public const string SVG_CLIP = "clip";
    public const string SVG_CLIP_PATH = "clip-path";
    public const string SVG_CLIP_RULE = "clip-rule";
    public const string SVG_FILL = "fill";
    public const string SVG_FILL_OPACITY = "fill-opacity";
    public const string SVG_FILL_RULE = "fill-rule";
    public const string SVG_OPACITY = "opacity";
    public const string SVG_PAINT_ORDER = "paint-order";
    public const string SVG_STROKE = "stroke";
    public const string SVG_STROKE_LINECAP = "stroke-linecap";
    public const string SVG_STROKE_LINEJOIN = "stroke-linejoin";
    public const string SVG_STROKE_OPACITY = "stroke-opacity";
    public const string SVG_STROKE_WIDTH = "stroke-width";
    public const string SVG_MASK = "mask";
    public const string SVG_POINTS = "points";

    public static readonly ImmutableDictionary<string, string> presentationMap =
        ImmutableDictionary.CreateRange([
            KeyValuePair.Create(SVG_CLIP, "android:clip"),
            KeyValuePair.Create(SVG_CLIP_RULE, ""), // Treated individually.
            KeyValuePair.Create(SVG_FILL, "android:fillColor"),
            KeyValuePair.Create(SVG_FILL_OPACITY, "android:fillAlpha"),
            KeyValuePair.Create(SVG_FILL_RULE, "android:fillType"),
            KeyValuePair.Create(SVG_OPACITY, ""), // Treated individually.
            KeyValuePair.Create(SVG_PAINT_ORDER, ""), // Treated individually.
            KeyValuePair.Create(SVG_STROKE, "android:strokeColor"),
            KeyValuePair.Create(SVG_STROKE_LINECAP, "android:strokeLineCap"),
            KeyValuePair.Create(SVG_STROKE_LINEJOIN, "android:strokeLineJoin"),
            KeyValuePair.Create(SVG_STROKE_OPACITY, "android:strokeAlpha"),
            KeyValuePair.Create(SVG_STROKE_WIDTH, "android:strokeWidth")
        ]);

    public static readonly ImmutableDictionary<string, string> gradientMap =
        ImmutableDictionary.CreateRange([
            KeyValuePair.Create("x1", "android:startX"),
            KeyValuePair.Create("y1", "android:startY"),
            KeyValuePair.Create("x2", "android:endX"),
            KeyValuePair.Create("y2", "android:endY"),
            KeyValuePair.Create("cx", "android:centerX"),
            KeyValuePair.Create("cy", "android:centerY"),
            KeyValuePair.Create("r", "android:gradientRadius"),
            KeyValuePair.Create("spreadMethod", "android:tileMode"),
            KeyValuePair.Create("gradientUnits", ""),
            KeyValuePair.Create("gradientTransform", ""),
            KeyValuePair.Create("gradientType", "android:type")
        ]);

    private static readonly ImmutableHashSet<string> unsupportedSvgNodes = ImmutableHashSet.Create(
            // Animation elements.
            "animate",
            "animateColor",
            "animateMotion",
            "animateTransform",
            "mpath",
            "set",
            // Container elements.
            "a",
            "marker",
            "missing-glyph",
            "pattern",
            "switch",
            // Filter primitive elements.
            "feBlend",
            "feColorMatrix",
            "feComponentTransfer",
            "feComposite",
            "feConvolveMatrix",
            "feDiffuseLighting",
            "feDisplacementMap",
            "feFlood",
            "feFuncA",
            "feFuncB",
            "feFuncG",
            "feFuncR",
            "feGaussianBlur",
            "feImage",
            "feMerge",
            "feMergeNode",
            "feMorphology",
            "feOffset",
            "feSpecularLighting",
            "feTile",
            "feTurbulence",
            // Font elements.
            "font",
            "font-face",
            "font-face-format",
            "font-face-name",
            "font-face-src",
            "font-face-uri",
            "hkern",
            "vkern",
            // Gradient elements.
            "stop",
            // Graphics elements.
            "ellipse",
            "image",
            // Light source elements.
            "feDistantLight",
            "fePointLight",
            "feSpotLight",
            // Structural elements.
            "symbol",
            // Text content elements.
            "altGlyphDef",
            "altGlyphItem",
            "glyph",
            "glyphRef",
            "text",
            // Text content child elements.
            "altGlyph",
            "textPath",
            "tref",
            "tspan",
            // Uncategorized elements.
            "color-profile",
            "cursor",
            "filter",
            "foreignObject",
            "script",
            "view");

    [GeneratedRegex("[\\s,]+")]
    private static partial Regex SPACE_OR_COMMA();

    private static SvgTree Parse(string file)
    {
        var xDoc = XDocument.Load(file);

        // Get <svg> elements.
        var rootElement = xDoc.Root ?? throw new InvalidDataException("Root element is null.");
        if (rootElement.Name != "svg") throw new InvalidDataException("Root element is not svg");

        var svgTree = new SvgTree();
        svgTree.ParseDimension(rootElement);
        if(svgTree.ViewBox == null)
        {
            throw new InvalidDataException("Missing \"viewBox\" in <svg> element.");
        }

        SvgGroupNode root = new SvgGroupNode(svgTree, rootElement, "root");

        // svgTree.parseDimension(rootElement);
        /*        if (svgTree.getViewBox() == null) {
            svgTree.logError("Missing \"viewBox\" in <svg> element", rootElement);
            return svgTree;
        }
         */


    }

    private static void TraverseSvgAndExtract(SvgTree svgTree, XElement element)
    {
        foreach (var (i, item) in element.Elements().Index())
        {
            // The node contains no information, just ignore it.
            if (!item.HasAttributes && item.HasElements) continue;

            var tagName = item.Name.LocalName;
            switch (tagName)
            {
                case SVG_PATH:
                case SVG_RECT:
                case SVG_CIRCLE:
                case SVG_ELLIPSE:
                case SVG_POLYGON:
                case SVG_POLYLINE:
                case SVG_LINE:

                    break;

                case SVG_GROUP:
                    break;

                case SVG_USE:
                    break;

                case SVG_DEFS:
                    break;

                case SVG_CLIP_PATH_ELEMENT:
                case SVG_MASK:
                    break;

                case SVG_STYLE: break;

                case "linearGradient":
                    break;
                case "radialGradient":
                    break;

                default: 
                    break;
            }
        } 
    }

    public static string ParseSvgToXml(string inputSvg, Stream outStream)
    {
        SvgTree svgTree = Parse(inputSvg);
        if (svgTree.HasLeafNode)
        {
            WriteFile(outStream, svgTree);
        }

        return "";
    }
}
