using System.Xml.Linq;
using Android.OS;
using iTextSharp.awt.geom;

namespace Jing8956.Maui.SvgImageSource.VectorDrawable;

internal abstract class SvgNode
{
    // private static final Logger logger = Logger.getLogger(SvgNode.class.getSimpleName());
    protected const string INDENT_UNIT = "  ";
    protected const string CONTINUATION_INDENT = INDENT_UNIT + INDENT_UNIT;
    private const string TRANSFORM_TAG = "transform";

    private const string MATRIX_ATTRIBUTE = "matrix";
    private const string TRANSLATE_ATTRIBUTE = "translate";
    private const string ROTATE_ATTRIBUTE = "rotate";
    private const string SCALE_ATTRIBUTE = "scale";
    private const string SKEWX_ATTRIBUTE = "skewX";
    private const string SKEWY_ATTRIBUTE = "skewY";

    protected readonly string? _name;
    // Keep a reference to the tree in order to dump the error log.
    protected readonly SvgTree _svgTree;
    // Use document element to get the line number for error reporting.
    protected readonly XElement _documentElement;

    // Key is the attributes for vector drawable, and the value is the converted from SVG.
    protected readonly Dictionary<string, string> _vdAttributesMap = [];
    // Stroke is applied before fill as a result of "paint-order:stroke fill" style.
    protected bool _strokeBeforeFill;
    // If mLocalTransform is identity, it is the same as not having any transformation.
    protected AffineTransform _localTransform = new();

    // During the flatten() operation, we need to merge the transformation from top down.
    // This is the stacked transformation. And this will be used for the path data transform().
    protected AffineTransform _stackedTransform = new();


    /// <summary>While parsing the translate() rotate() ..., update the <c>_localTransform</c>.</summary>
    public SvgNode(SvgTree svgTree, XElement element, string? name)
    {
        _name = name;
        _svgTree = svgTree;
        _documentElement = element;

        // Parse and generate a presentation map.
        foreach (var item in element.Attributes())
        {
            var nodeName = item.Name.LocalName;
            var nodeValue = item.Value;

            // TODO: Handle style here. Refer to Svg2Vector::addStyleToPath().

        }
    }

}
