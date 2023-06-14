using System.ComponentModel;
using System.Xml.Serialization;

namespace Bnhp.Ocr;

public class OcrNode
{
    public override string? ToString() => text;

    [System.Text.Json.Serialization.JsonIgnore]
    [XmlIgnore]
    public OcrNode? parent { get; set; }

    public List<OcrNode>? children { get; set; }

    [DefaultValue(0)]
    public int angle { get; set; }

    /**
     * 1 - page
     * 2 - block
     * 3 - paragraph
     * 4 - line
     * 5 - word
     */
    public int level { get; set; }

    public int pageNum { get; set; }
    public int blockNum { get; set; }
    public int parNum { get; set; }
    public int lineNum { get; set; }
    public int wordNum { get; set; }
    public int left { get; set; }
    public int top { get; set; }
    public int width { get; set; }
    public int height { get; set; }

    public float? confidence { get; set; }
    public string? text { get; set; }
    public List<OcrChoice>? choices { get; set; }
}

public class OcrChoice
{
    public float? confidence { get; set; }
    public string? text { get; set; }
}
