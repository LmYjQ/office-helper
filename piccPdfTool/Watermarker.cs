using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Vml.Wordprocessing;
using DocumentFormat.OpenXml.Vml.Office;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;

namespace piccPdfTool
{
    public class Watermarker
    {
        /// <summary>
        /// 添加 VML 文字水印（斜向半透明，Word 原生效果）
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="text">水印文字</param>
        /// <param name="color">颜色（十六进制，如 #C0C0C0）</param>
        /// <param name="fontSize">字体大小（磅）</param>
        /// <param name="angle">旋转角度（负数为逆时针）</param>
        public void AddTextWatermark(string inputPath, string outputPath, string text,
            string color = "#C0C0C0", int fontSize = 60, int angle = -45)
        {
            File.Copy(inputPath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart!;

                // 创建 VML 文字水印（兼容 Word 2007+）
                var watermarkPara = CreateVmlWatermark(text, color, fontSize, angle);

                // 创建页眉部件
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = new Header(watermarkPara);
                string headerId = mainPart.GetIdOfPart(headerPart);

                // 应用到所有节
                var body = mainPart.Document!.Body!;
                var sections = body.Elements<SectionProperties>().ToList();

                if (!sections.Any())
                {
                    // 无分节时创建默认节属性
                    body.Append(new SectionProperties(
                        new HeaderReference { Id = headerId, Type = HeaderFooterValues.Default }
                    ));
                }
                else
                {
                    foreach (var sectPr in sections)
                    {
                        // 移除现有页眉引用
                        sectPr.RemoveAllChildren<HeaderReference>();
                        sectPr.PrependChild(new HeaderReference
                        {
                            Id = headerId,
                            Type = HeaderFooterValues.Default
                        });
                    }
                }

                mainPart.Document.Save();
            }
        }

        /// <summary>
        /// 创建 VML 文字水印（支持旋转角度和位置）
        /// </summary>
        private Paragraph CreateVmlWatermark(string text, string color, int fontSize, int angle, bool isHeader = false)
        {
            // VML 形状定义（Word 原生水印格式）
            var shape = new V.Shape()
            {
                Id = "_x0000_s2049",
                Style = BuildVmlStyle(angle, isHeader),
                CoordinateSize = "9360,4680",
                OptionalString = "_x0000_t136",
                FillColor = color,
                Stroked = false,
                Type = "#_x0000_t136"
            };

            // 文字路径
            var textPath = new V.TextPath()
            {
                Style = $"font-family:\"Microsoft YaHei\";font-size:{fontSize}pt",
                String = text
            };

            // 填充设置（半透明）
            var fill = new V.Fill() { Opacity = isHeader ? ".6" : ".5" };
            shape.Append(fill);
            shape.Append(textPath);

            var pict = new Picture(shape);
            var run = new Run(pict);

            return new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = "Header" }
                ),
                run
            );
        }

        private string BuildVmlStyle(int angle, bool isHeader = false)
        {
            if (isHeader)
            {
                // 页眉水印：不旋转，靠近顶部居中
                return $"position:absolute;margin-left:0;margin-top:5pt;width:468pt;height:50pt;" +
                    $"rotation:0;z-index:-251658752;" +
                    $"mso-position-horizontal:center;mso-position-horizontal-relative:margin;" +
                    $"mso-position-vertical:top;mso-position-vertical-relative:margin";
            }
            // 正文水印：旋转，居中
            return $"position:absolute;margin-left:0;margin-top:0;width:468pt;height:234pt;" +
                $"rotation:{angle};z-index:-251658752;" +
                $"mso-position-horizontal:center;mso-position-horizontal-relative:margin;" +
                $"mso-position-vertical:center;mso-position-vertical-relative:margin";
        }

        /// <summary>
        /// 添加图片水印（使用 DrawingML，更现代）
        /// </summary>
        public void AddImageWatermark(string inputPath, string outputPath, string imagePath)
        {
            File.Copy(inputPath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart!;
                var imagePart = mainPart.AddImagePart(ImagePartType.Png);

                using (FileStream stream = new FileStream(imagePath, FileMode.Open))
                {
                    imagePart.FeedData(stream);
                }

                // 构建 DrawingML 图片水印
                var drawing = BuildImageWatermarkDrawing(mainPart.GetIdOfPart(imagePart));
                var headerPart = mainPart.AddNewPart<HeaderPart>();

                headerPart.Header = new Header(
                    new Paragraph(
                        new ParagraphProperties(new ParagraphStyleId { Val = "Header" }),
                        new Run(drawing)
                    )
                );

                ApplyHeaderToAllSections(mainPart, headerPart);
                mainPart.Document!.Save();
            }
        }

        private Drawing BuildImageWatermarkDrawing(string relationshipId)
        {
            uint id = 1;
            var blip = new A.Blip { Embed = relationshipId };

            var picture = new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new A.NonVisualDrawingProperties { Id = 0, Name = "Watermark.png" },
                    new A.NonVisualPictureDrawingProperties()
                ),
                new PIC.BlipFill(blip, new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0, Y = 0 },
                        new A.Extents { Cx = 4680000, Cy = 2340000 } // EMUs
                    ),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                )
            );

            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = 4680000, Cy = 2340000 },
                    new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                    new DW.DocProperties { Id = id, Name = "Watermark" },
                    new DW.NonVisualGraphicFrameDrawingProperties(),
                    picture
                ) { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 }
            );
        }

        private void ApplyHeaderToAllSections(MainDocumentPart mainPart, HeaderPart headerPart)
        {
            var headerId = mainPart.GetIdOfPart(headerPart);
            var body = mainPart.Document!.Body!;

            foreach (var sectPr in body.Elements<SectionProperties>())
            {
                sectPr.RemoveAllChildren<HeaderReference>();
                sectPr.PrependChild(new HeaderReference
                {
                    Id = headerId,
                    Type = HeaderFooterValues.Default
                });
            }
        }

        /// <summary>
        /// 添加 VML 格式水印（循环显示多个水印文字）
        /// </summary>
        public void AddVmlWatermarkLoop(string inputPath, string outputPath,
            string[] watermarkTexts, int fontSize)
        {
            File.Copy(inputPath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart!;

                // 创建多个 VML 水印（循环显示）
                var watermarkPara = CreateVmlWatermarkLoop(watermarkTexts, "#C0C0C0", fontSize, -30);

                // 创建页眉部件
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = new Header(watermarkPara);
                string headerId = mainPart.GetIdOfPart(headerPart);

                // 应用到所有节
                var body = mainPart.Document!.Body!;
                var sections = body.Elements<SectionProperties>().ToList();

                if (!sections.Any())
                {
                    body.Append(new SectionProperties(
                        new HeaderReference { Id = headerId, Type = HeaderFooterValues.Default }
                    ));
                }
                else
                {
                    foreach (var sectPr in sections)
                    {
                        sectPr.RemoveAllChildren<HeaderReference>();
                        sectPr.PrependChild(new HeaderReference
                        {
                            Id = headerId,
                            Type = HeaderFooterValues.Default
                        });
                    }
                }

                mainPart.Document.Save();
            }
        }

        /// <summary>
        /// 创建循环显示多个水印文字的 VML 段落
        /// </summary>
        private Paragraph CreateVmlWatermarkLoop(string[] texts, string color, int fontSize, int angle)
        {
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties(
                new ParagraphStyleId { Val = "Header" }
            );
            paragraph.Append(paragraphProperties);

            // 为每个水印文字创建一个 VML 形状
            int index = 0;
            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // 错开每个水印的位置
                int offsetX = (index % 3) * 200;
                int offsetY = (index / 3) * 300;

                var shape = new V.Shape()
                {
                    Id = $"_x0000_s{2049 + index}",
                    Style = $"position:absolute;margin-left:{offsetX};margin-top:{offsetY};width:600pt;height:200pt;rotation:{angle};z-index:-251658752;mso-position-horizontal:center;mso-position-horizontal-relative:margin;mso-position-vertical:center;mso-position-vertical-relative:margin",
                    CoordinateSize = "9360,4680",
                    OptionalString = "_x0000_t136",
                    FillColor = color,
                    Stroked = false,
                    Type = "#_x0000_t136"
                };

                var textPath = new V.TextPath()
                {
                    Style = $"font-family:\"Microsoft YaHei\";font-size:{fontSize}pt",
                    String = text.Trim()
                };

                var fill = new V.Fill() { Opacity = ".4" };
                shape.Append(fill);
                shape.Append(textPath);

                var pict = new Picture(shape);
                var run = new Run(pict);
                paragraph.Append(run);

                index++;
            }

            return paragraph;
        }

        /// <summary>
        /// 同时添加页眉水印和正文水印
        /// - 页眉水印（商业秘密）：作为普通文本添加到页眉，不旋转，显示在页面顶部
        /// - 正文水印：循环显示在页眉区域，旋转
        /// </summary>
        public void AddBothWatermarks(string inputPath, string outputPath,
            string? headerText, int headerFontSize,
            string[]? bodyTexts, int bodyFontSize)
        {
            File.Copy(inputPath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart!;
                var body = mainPart.Document!.Body!;

                // 创建页眉
                var header = new Header();

                // 1. 添加页眉水印（普通文本，不旋转，显示在页面顶部）
                if (!string.IsNullOrEmpty(headerText))
                {
                    var headerTextPara = CreateHeaderTextWatermark(headerText, "#808080", headerFontSize);
                    header.Append(headerTextPara);
                }

                // 2. 添加正文水印（循环多个，旋转）到页眉
                if (bodyTexts != null && bodyTexts.Length > 0)
                {
                    var bodyWatermarkPara = CreateVmlWatermarkLoop(bodyTexts, "#C0C0C0", bodyFontSize, -30);
                    header.Append(bodyWatermarkPara);
                }

                // 将页眉添加到文档
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = header;
                string headerId = mainPart.GetIdOfPart(headerPart);

                // 应用到所有节
                var sections = body.Elements<SectionProperties>().ToList();
                if (!sections.Any())
                {
                    body.Append(new SectionProperties(
                        new HeaderReference { Id = headerId, Type = HeaderFooterValues.Default }
                    ));
                }
                else
                {
                    foreach (var sectPr in sections)
                    {
                        sectPr.RemoveAllChildren<HeaderReference>();
                        sectPr.PrependChild(new HeaderReference
                        {
                            Id = headerId,
                            Type = HeaderFooterValues.Default
                        });
                    }
                }

                mainPart.Document.Save();
            }
        }

        /// <summary>
        /// 创建页眉普通文本（不使用 VML，直接显示在页眉区域）
        /// </summary>
        private Paragraph CreateHeaderTextWatermark(string text, string color, int fontSize)
        {
            // 将十六进制颜色转换为 Word 颜色值
            var colorValue = color.Replace("#", "");

            var run = new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Microsoft YaHei", HighAnsi = "Microsoft YaHei" },
                    new FontSize { Val = new StringValue((fontSize * 2).ToString()) }, // Word uses half-points
                    new DocumentFormat.OpenXml.Wordprocessing.Color { Val = colorValue }
                ),
                new Text(text)
            );

            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = "Header" },
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "0", Before = "0", Line = "240" }
                ),
                run
            );

            return paragraph;
        }
    }
}
