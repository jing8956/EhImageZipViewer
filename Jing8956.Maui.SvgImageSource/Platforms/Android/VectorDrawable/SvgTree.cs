using System.Buffers;
using System.util.collections;
using System.Xml.Linq;
using iTextSharp.awt.geom;
using Java.Text;
using Org.W3c.Dom;

namespace Jing8956.Maui.SvgImageSource.VectorDrawable;

internal class SvgTree
{
    private const string HEAD = "<vector xmlns:android=\"http://schemas.android.com/apk/res/android\"";
    private const string AAPT_BOUND = "xmlns:aapt=\"http://schemas.android.com/aapt\"";

    public const string  SVG_WIDTH = "width";
    public const string  SVG_HEIGHT = "height";
    public const string  SVG_VIEW_BOX = "viewBox";

    private float _w = -1.0f;
    private float _h = -1.0f;
    private readonly AffineTransform _rootTransform = new();
    private float[] _viewBox;

    private SvgGroupNode _root;
    private string _fileName;

    private readonly List<LogMessage> _logMessages = [];

    private bool _hasLeafNode;
    private bool _hasGradient;

    /** Map of SvgNode's id to the SvgNode. */
    private readonly Dictionary<string, SvgNode> _idMap = [];

    /** IDs of ignored SVG nodes. */
    private readonly HashSet<string> _ignoredIds = [];

    /** Set of SvgGroupNodes that contain "use" elements. */
    private readonly HashSet<SvgGroupNode> _pendingUseGroupSet = [];

    /** Set of SvgGradientNodes that contain "href"" elements. */
    private readonly HashSet<SvgGradientNode> _endingGradientRefSet = [];

    /**
     * Key is SvgNode that references a clipPath. Value is SvgGroupNode that is the parent of that
     * SvgNode.
     */
    private readonly LinkedDictionary<SvgNode, (SvgGroupNode, string)> _clipPathAffectedNodes = [];

    /**
     * Key is String that is the id of a style class. Value is set of SvgNodes referencing that
     * class.
     */
    private readonly Dictionary<string, HashSet<SvgNode>> _styleAffectedNodes = [];

    /**
     * Key is String that is the id of a style class. Value is a String that contains attribute
     * information of that style class.
     */
    private readonly Dictionary<string, string> _styleClassAttributeMap = [];

    private NumberFormat? _coordinateFormat;

    private enum SvgLogLevel
    {
        ERROR,
        WARNING
    }

    private record class LogMessage(SvgLogLevel Level, int Line, string Message)
    {
        public override string ToString()
        {
            return $"{Level} @ line {Line}: {Message}";
        }
    }

    public float Width => _w;
    public float Height => _h;
    public float ScaleFactor => 1f;

    public bool HasLeafNode { get => _hasLeafNode; set => _hasLeafNode = value; }
    public bool HasGradient { get => _hasLeafNode; set => _hasGradient = value; }

    public float[] ViewBox => _viewBox;

    /** From the root, top down, pass the transformation (TODO: attributes) down the children. */
    public void Flatten() => _root.Flatten(new AffineTransform());
    /** Validates all nodes and logs any encountered issues. */
    public void Validate()
    {
        _root.Validate();
        if(_logMessages.Count == 0 && !HasLeafNode)
        {
            LogError("No vector content found", null);
        }
    }

    private enum SizeType
    {
        PIXEL,
        PERCENTAGE
    }

    private readonly SearchValues<string> _unitSearchValues = SearchValues.Create(
        ["em", "ex", "px", "in", "cm", "mm", "pt", "pc"], StringComparison.Ordinal);
    public void ParseDimension(XElement element)
    {
        var widthType = SizeType.PIXEL;
        var heightType = SizeType.PIXEL;

        foreach (var item in element.Attributes())
        {
            var name = item.Name.LocalName;
            var value = item.Value;
            var currentType = SizeType.PIXEL;
            var unit = value.Length > 2 ? value[^2..] : "";

            var subValueSize = value.Length;
            if(_unitSearchValues.Contains(unit))
            {
                subValueSize -= 2;
            }
            else if (unit.EndsWith('%'))
            {
                subValueSize -= 1;
                currentType = SizeType.PERCENTAGE;
            }

            var subValue = value[..subValueSize];
            if (string.Equals(SVG_WIDTH, name, StringComparison.Ordinal))
            {
                _w = float.Parse(subValue);
                widthType = currentType;
            }
            else if(string.Equals(SVG_HEIGHT, name, StringComparison.Ordinal))
            {
                _h = float.Parse(subValue);
                heightType = currentType;
            }
            else if(string.Equals(SVG_VIEW_BOX, name, StringComparison.Ordinal))
            {
                _viewBox = new float[4];
                var strBox = value.Split(' ');
                for (int j = 0; j < _viewBox.Length; j++)
                {
                    _viewBox[j] = float.Parse(strBox[j]);
                }
            }
        }
        
        // If there is no viewbox, then set it up according to w, h.
        // From now on, viewport should be read from viewBox, and size should be from w and h.
        // w and h can be set to percentage too, in this case, set it to the viewbox size.
        if(_viewBox == null && _w > 0 && _h > 0)
        {
            _viewBox = [0f, 0f, _w, _h];
        }
        else if ((_w < 0 || _h < 0) && _viewBox != null)
        {
            _w = _viewBox[2]; // _viewBox[0] == 0 ?
            _h = _viewBox[3]; // _viewBox[1] == 0 ?
        }

        if (widthType == SizeType.PERCENTAGE && _w > 0 && _viewBox != null)
        {
            _w = _viewBox[2] * _w / 100; // _viewBox[0] == 0 ?
        }
        if (heightType == SizeType.PERCENTAGE && _h > 0 && _viewBox != null)
        {
            _h = _viewBox[3] * _h / 100; // _viewBox[1] == 0 ?
        }
    }

    public XDocument Parse(string file)
    {
        return XDocument.Load(file);
    }

    public void Normalize()
    {
        _rootTransform.preConcatenate(new AffineTransform(1, 0, 0, 1, -_viewBox[0], -_viewBox[1]));
        Transform(_rootTransform);

        // logger.log(Level.FINE, "matrix=" + mRootTransform);
    }

    public void Transform(AffineTransform rootTransform)
    {
        _root.TransformIfNeeded(rootTransform);
    }

    public void Dump()
    {
        // logger.log(Level.FINE, "file: " + mFileName);
        _root.DumpNode("");
    }

    public SvgGroupNode? Root { get => _root; set => _root = value; }

    public void LogError(string s, Node? node)
    {
        LogErrorLine(s, node, SvgLogLevel.ERROR);
    }
    public void LogWarning(string s, Node? node)
    {
        logErrorLine(s, node, SvgLogLevel.WARNING);
    }

    public void logErrorLine(string s, Node? node, SvgLogLevel level)
    {
        if (s.Length == 0) throw new ArgumentException("Argument 's' can not empty.", nameof(s));
        var line = node == null ? 0 : GetStartLine(node);
        _logMessages.Add(new LogMessage(level, line, s));
    }


}
