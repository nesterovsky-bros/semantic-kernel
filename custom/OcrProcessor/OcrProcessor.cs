using System.Reflection;
using System.Runtime.InteropServices;

using Docnet.Core;
using Docnet.Core.Models;

using OpenCvSharp;

using Tesseract;

namespace Bnhp.Ocr;

public class OcrProcessor
{
    public static IEnumerable<OcrNode> Run(string contentType, byte[] data)
    {
        using var resource = AcquireTesseract();
        var engine = resource.Engine;
        var pdf = contentType.Contains("pdf");

        var input = pdf ?
            GetPdfPages(data) :
            new[] { (width: 0, height: 0, data, content: null as string) };

        static Mat getImage(bool pdf, int width, int height, byte[] data)
        {
            if (pdf)
            {
                using var image = new Mat(height, width, MatType.CV_8UC4);

                Marshal.Copy(data, 0, image.Data, data.Length);

                return image.CvtColor(ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
                return Mat.FromImageData(data, ImreadModes.Grayscale);
            }
        }

        foreach (var item in input)
        {
            using var mat = getImage(pdf, item.width, item.height, item.data);
            using var normalized = Scale(mat, 1600);

            //using var w = new Window("Page", normalized, WindowFlags.Normal | WindowFlags.AutoSize);

            //Cv2.WaitKey();

            var ocrNode = RunOcr(engine, normalized, 193);

            yield return ocrNode;
        }
    }

    private static Mat Scale(Mat input, int width)
    {
        if ((input.Width == 0) || (input.Height == 0))
        {
            return input;
        }

        var scale = (double)width / input.Width;

        return input.Resize(
          Size.Zero,
          scale,
          scale,
          InterpolationFlags.Area);
    }

    private static IEnumerable<(int width, int height, byte[] data, string? content)> GetPdfPages(byte[] fileData)
    {
        using var reader = DocLib.Instance.GetDocReader(fileData, new PageDimensions(3200, 4528));
        var count = reader.GetPageCount();

        for (var index = 0; index < count; ++index)
        {
            using var page = reader.GetPageReader(index);

            var width = page.GetPageWidth();
            var height = page.GetPageHeight();
            var data = page.GetImage();
            var content = page.GetText();

            for (var i = 0; i < data.Length; i += 4)
            {
                if ((data[i] == 0) && (data[i + 1] == 0) && (data[i + 2] == 0) && (data[i + 3] == 0))
                {
                    data[i] = 255;
                    data[i + 1] = 255;
                    data[i + 2] = 255;
                    data[i + 3] = 255;
                }
            }

            yield return (width, height, data, content);
        }
    }

    private unsafe static OcrNode RunOcr(
      TesseractEngine engine,
      Mat input,
      int resolution)
    {
        using var pix = Pix.Create(input.Width, input.Height, 8);

        pix.XRes = resolution;
        pix.YRes = resolution;

        var pixData = pix.GetData();
        var data = (uint*)pixData.Data;
        var wordsPerLine = pixData.WordsPerLine;

        input.ForEachAsByte((value, position) =>
        {
            var y = position[0];
            var x = position[1];
            var color = *value;

            PixData.SetDataByte(data + y * wordsPerLine, x, color);
        });

        var lineSeparator = new[] { '\n' };
        var columnSeparator = new[] { '\t' };

        using var page = engine.Process(pix);

        var text = page.GetTsvText(1);

        var ocrPage = text.
          Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries).
          Select(line => line.Split(columnSeparator)).
          Select(columns => new OcrNode
          {
              level = int.Parse(columns[0]),
              pageNum = int.Parse(columns[1]),
              blockNum = int.Parse(columns[2]),
              parNum = int.Parse(columns[3]),
              lineNum = int.Parse(columns[4]),
              wordNum = int.Parse(columns[5]),
              left = int.Parse(columns[6]),
              top = int.Parse(columns[7]),
              width = int.Parse(columns[8]),
              height = int.Parse(columns[9]),
              confidence = NullIf(float.Parse(columns[10]), -1),
              text = columns.Length > 11 ? columns[11] : null
          }).
          Aggregate((prev, next) =>
          {
              if (prev == null)
              {
                  if (next.level != 1)
                  {
                      throw new InvalidOperationException("nodes");
                  }
              }
              else
              {
                  if ((next.level <= 0) || (next.level > prev.level + 1))
                  {
                      throw new InvalidOperationException("nodes");
                  }

                  while (next.level != prev.level + 1)
                  {
                      prev = prev.parent!;
                  }

                  next.parent = prev;

                  if (prev.children == null)
                  {
                      prev.children = new List<OcrNode>();
                  }

                  prev.children.Add(next);
              }

              return next;
          });

        while (ocrPage.parent != null)
        {
            ocrPage = ocrPage.parent;
        }

        ocrPage.text = page.GetText();

        return ocrPage;
    }

    private static T? NullIf<T>(T value, T nullValue)
      where T : struct, IComparable<T> =>
      value.CompareTo(nullValue) == 0 ? null : value;

    private static TesseractResource AcquireTesseract() => new();

    private class TesseractResource : IDisposable
    {
        static TesseractResource()
        {
            var basePath = Path.GetDirectoryName(
              Uri.UnescapeDataString(
                new UriBuilder(Assembly.GetExecutingAssembly().Location).Path))!;

            var traindata = Path.Combine(basePath, "traindata");

            if (!Directory.Exists(traindata))
            {
                traindata = Path.Combine(basePath, "..", "traindata");

                if (!Directory.Exists(traindata))
                {
                    throw new InvalidOperationException("No traindata is found.");
                }
            }

            engine = new TesseractEngine(traindata, "eng+heb");
            //engine = new TesseractEngine(traindata, "eng");
        }

        public TesseractResource()
        {
        }

        public void Dispose()
        {
        }

        public TesseractEngine Engine => engine;

        private static readonly TesseractEngine engine;
    }
}
