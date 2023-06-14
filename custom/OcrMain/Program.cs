using System.Text.Json;
using System.Text.RegularExpressions;

using Bnhp.Ocr;

var input = args[0];
var output = args[1];

var type = Path.GetExtension(input);
var data = File.ReadAllBytes(input);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};


foreach(var item in OcrProcessor.Run(type, data).Select((node, index) => (node, index)))
{
    Console.WriteLine($"Page: {item.index + 1}");
    // File.AppendAllText(output, JsonSerializer.Serialize(node, jsonOptions));

    var page = string.Join(
        "\n\n",
        Flatten(item.node).
            GroupBy(node => (node.pageNum, node.blockNum, node.parNum)).
            OrderBy(group => ((group.Key.pageNum, group.Key.blockNum, group.Key.parNum))).
            Select(group =>
                string.Join(
                    "\n",
                    group.
                        GroupBy(node => node.lineNum).
                        OrderBy(group => group.Key).
                        Select(group => string.Join(" ", group.OrderBy(node => node.wordNum)))))) +
         "\n\n\n\n";

    File.AppendAllText(output, page);
}

IEnumerable<OcrNode> Flatten(OcrNode node)
{
    if (node.level > 1 && !string.IsNullOrWhiteSpace(node.text))
    {
        yield return node;
    }

    if (node.children != null)
    {
        foreach(var child in node.children)
        {
            foreach(var flatten in Flatten(child))
            {
                yield return flatten;
            }
        }
    }
}
