// -------------------------------------------------------------------------
// 1. 引入必要的命名空间
// -------------------------------------------------------------------------
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Open XML SDK 的命名空间
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing; // A 是 DrawingML 的别名，后面会用到

namespace piccPdfTool
{
    public partial class Form1 : Form
    {
        // -------------------------------------------------------------------------
        // 2. 声明界面上的控件
        // -------------------------------------------------------------------------
        private TextBox txtFilePath = null!;
        private Button btnSelectFile = null!;
        private Label lblWatermark = null!;
        private TextBox txtWatermarks = null!;
        private Button btnProcess = null!;
        private Label lblStatus = null!;

        // --- 新增控件：字体大小输入框 ---
        private Label lblFontSize = null!;
        private NumericUpDown numFontSize = null!;

        // --- 新增控件：Word文件选择按钮 ---
        private Button btnSelectWordFile = null!;
        private string currentFileType = "pptx"; // 当前选择的文件类型：pptx 或 docx

        // --- 新增控件：水印选项CheckBox ---
        private System.Windows.Forms.CheckBox chkHeaderWatermark = null!;
        private System.Windows.Forms.CheckBox chkBodyWatermark = null!;
        private Label lblBodyFontSize = null!;
        private NumericUpDown numBodyFontSize = null!;

        // --- 新增控件：页眉固定文字 ---
        private Label lblHeaderText = null!;
        private TextBox txtHeaderText = null!;

        // --- VML 水印器 ---
        private Watermarker watermarker = new Watermarker();

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "PPT/Word 批量加水印工具";
            this.Size = new Size(600, 380);

            // --- 文件选择部分 ---
            btnSelectFile = new Button();
            btnSelectFile.Text = "选择PPT文件";
            btnSelectFile.Location = new Point(12, 12);
            btnSelectFile.Size = new Size(100, 25);
            btnSelectFile.Click += new EventHandler(BtnSelectFile_Click);
            this.Controls.Add(btnSelectFile);

            btnSelectWordFile = new Button();
            btnSelectWordFile.Text = "选择Word文件";
            btnSelectWordFile.Location = new Point(120, 12);
            btnSelectWordFile.Size = new Size(100, 25);
            btnSelectWordFile.Click += new EventHandler(BtnSelectWordFile_Click);
            this.Controls.Add(btnSelectWordFile);

            txtFilePath = new TextBox();
            txtFilePath.Location = new Point(230, 14);
            txtFilePath.Size = new Size(340, 20);
            txtFilePath.ReadOnly = true;
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtFilePath);

            // --- 水印输入部分 ---
            lblWatermark = new Label();
            lblWatermark.Text = "水印文字(逗号分隔):";
            lblWatermark.Location = new Point(12, 50);
            lblWatermark.AutoSize = true;
            this.Controls.Add(lblWatermark);

            txtWatermarks = new TextBox();
            txtWatermarks.Location = new Point(12, 70);
            txtWatermarks.Size = new Size(560, 20);
            txtWatermarks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtWatermarks.Text = "北京,黑龙江,乌鲁木齐";
            this.Controls.Add(txtWatermarks);

            // --- 水印选项部分 ---
            chkHeaderWatermark = new System.Windows.Forms.CheckBox();
            chkHeaderWatermark.Text = "页眉水印";
            chkHeaderWatermark.Location = new Point(12, 100);
            chkHeaderWatermark.AutoSize = true;
            chkHeaderWatermark.Checked = true;
            this.Controls.Add(chkHeaderWatermark);

            chkBodyWatermark = new System.Windows.Forms.CheckBox();
            chkBodyWatermark.Text = "正文水印";
            chkBodyWatermark.Location = new Point(120, 100);
            chkBodyWatermark.AutoSize = true;
            chkBodyWatermark.Checked = true;
            this.Controls.Add(chkBodyWatermark);

            // --- 页眉设置部分 (第一行) ---
            lblFontSize = new Label();
            lblFontSize.Text = "页眉字体:";
            lblFontSize.Location = new Point(12, 130);
            lblFontSize.AutoSize = true;
            this.Controls.Add(lblFontSize);

            numFontSize = new NumericUpDown();
            numFontSize.Location = new Point(80, 128);
            numFontSize.Size = new Size(60, 22);
            numFontSize.Font = new System.Drawing.Font(this.Font.FontFamily, 9);
            numFontSize.Minimum = 10;
            numFontSize.Maximum = 200;
            numFontSize.Value = 20;
            this.Controls.Add(numFontSize);

            lblHeaderText = new Label();
            lblHeaderText.Text = "页眉文字:";
            lblHeaderText.Location = new Point(160, 130);
            lblHeaderText.AutoSize = true;
            this.Controls.Add(lblHeaderText);

            txtHeaderText = new TextBox();
            txtHeaderText.Location = new Point(230, 128);
            txtHeaderText.Size = new Size(100, 20);
            txtHeaderText.Text = "商业秘密";
            this.Controls.Add(txtHeaderText);

            // --- 正文设置部分 (第二行) ---
            lblBodyFontSize = new Label();
            lblBodyFontSize.Text = "正文字体:";
            lblBodyFontSize.Location = new Point(12, 165);
            lblBodyFontSize.AutoSize = true;
            this.Controls.Add(lblBodyFontSize);

            numBodyFontSize = new NumericUpDown();
            numBodyFontSize.Location = new Point(80, 163);
            numBodyFontSize.Size = new Size(60, 22);
            numBodyFontSize.Font = new System.Drawing.Font(this.Font.FontFamily, 9);
            numBodyFontSize.Minimum = 10;
            numBodyFontSize.Maximum = 200;
            numBodyFontSize.Value = 40;
            this.Controls.Add(numBodyFontSize);

            // --- 执行按钮 ---
            btnProcess = new Button();
            btnProcess.Text = "开始处理";
            btnProcess.Location = new Point(230, 200);
            btnProcess.Size = new Size(120, 35);
            btnProcess.Font = new System.Drawing.Font(this.Font.FontFamily, 10, FontStyle.Bold);
            btnProcess.Click += new EventHandler(BtnProcess_Click);
            this.Controls.Add(btnProcess);

            // --- 状态标签 ---
            lblStatus = new Label();
            lblStatus.Text = "请先选择一个文件";
            lblStatus.Location = new Point(12, 250);
            lblStatus.Size = new Size(560, 23);
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            this.Controls.Add(lblStatus);
        }

        private void BtnSelectFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PowerPoint 文件 (*.pptx)|*.pptx";
                openFileDialog.Title = "选择一个PPTX文件";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    currentFileType = "pptx";
                    lblStatus.Text = "PPT文件已选择，请输入水印并开始处理。";
                }
            }
        }

        private void BtnSelectWordFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Word 文件 (*.docx)|*.docx";
                openFileDialog.Title = "选择一个Word文件";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    currentFileType = "docx";
                    lblStatus.Text = "Word文件已选择，请输入水印并开始处理。";
                }
            }
        }

        private void BtnProcess_Click(object? sender, EventArgs e)
        {
            string originalFilePath = txtFilePath.Text;
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
            {
                MessageBox.Show("请先选择一个有效的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string watermarksInput = txtWatermarks.Text;
            if (string.IsNullOrEmpty(watermarksInput))
            {
                MessageBox.Show("请输入至少一个水印文本！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Word文档需要至少选择一个水印类型
            if (currentFileType == "docx" && !chkHeaderWatermark.Checked && !chkBodyWatermark.Checked)
            {
                MessageBox.Show("请至少选择一个水印类型（页眉或正文）！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int headerFontSize = (int)numFontSize.Value;
            int bodyFontSize = (int)numBodyFontSize.Value;
            string headerFixedText = txtHeaderText.Text.Trim();
            if (string.IsNullOrEmpty(headerFixedText))
            {
                headerFixedText = "商业秘密"; // 默认值
            }

            btnProcess.Enabled = false;
            lblStatus.Text = "正在处理中，请稍候...";
            Application.DoEvents();

            try
            {
                string[] watermarks = watermarksInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int successCount = 0;

                string? directory = Path.GetDirectoryName(originalFilePath);
                if (directory == null)
                {
                    throw new DirectoryNotFoundException("无法获取文件的目录路径。");
                }
                string originalFileName = Path.GetFileNameWithoutExtension(originalFilePath);

                // 根据文件类型选择扩展名和处理方法
                string extension = currentFileType == "pptx" ? ".pptx" : ".docx";

                foreach (string watermarkText in watermarks)
                {
                    string trimmedWatermark = watermarkText.Trim();
                    if (string.IsNullOrEmpty(trimmedWatermark)) continue;

                    string newFileName = $"{originalFileName}_{trimmedWatermark}{extension}";
                    string newFilePath = Path.Combine(directory, newFileName);

                    lblStatus.Text = $"正在为 \"{trimmedWatermark}\" 添加水印...";
                    Application.DoEvents();

                    if (currentFileType == "pptx")
                    {
                        AddWatermarkToPptx(newFilePath, trimmedWatermark, headerFontSize);
                    }
                    else
                    {
                        // Word文档处理
                        if (chkBodyWatermark.Checked && chkHeaderWatermark.Checked)
                        {
                            // 两个都勾选：同时添加页眉和正文水印
                            watermarker.AddBothWatermarks(
                                originalFilePath,
                                newFilePath,
                                headerFixedText,
                                headerFontSize,
                                new string[] { trimmedWatermark },
                                bodyFontSize
                            );
                        }
                        else if (chkBodyWatermark.Checked)
                        {
                            // 仅正文水印：使用 VML 格式
                            watermarker.AddVmlWatermarkLoop(
                                originalFilePath,
                                newFilePath,
                                new string[] { trimmedWatermark },
                                bodyFontSize
                            );
                        }
                        else if (chkHeaderWatermark.Checked)
                        {
                            // 仅页眉水印
                            watermarker.AddTextWatermark(
                                originalFilePath,
                                newFilePath,
                                headerFixedText,
                                "#C0C0C0",
                                headerFontSize,
                                -45
                            );
                        }
                    }
                    successCount++;
                }

                lblStatus.Text = $"处理完成！成功生成 {successCount} 个带水印的文件。";
                MessageBox.Show($"处理完成！\n成功生成 {successCount} 个文件，保存在原文件目录下。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "处理失败！";
                MessageBox.Show($"处理过程中发生错误：\n{ex.Message}", "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnProcess.Enabled = true;
            }
        }

        private void AddWatermarkToPptx(string filePath, string watermarkText, int fontSize)
        {
            using (PresentationDocument presentationDocument = PresentationDocument.Open(filePath, true))
            {
                PresentationPart? presentationPart = presentationDocument.PresentationPart;
                if (presentationPart == null) return;

                foreach (SlideMasterPart slideMasterPart in presentationPart.SlideMasterParts)
                {
                    var shapeTree = slideMasterPart.SlideMaster?.CommonSlideData?.ShapeTree;
                    if (shapeTree == null) continue;

                    Shape watermarkShape = new Shape();

                    watermarkShape.NonVisualShapeProperties = new NonVisualShapeProperties(
                        new NonVisualDrawingProperties() { Id = (uint)new Random().Next(10000, 99999), Name = $"Watermark_{watermarkText}" },
                        new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true, NoSelection = true }),
                        new ApplicationNonVisualDrawingProperties());

                    watermarkShape.ShapeProperties = new ShapeProperties(
                        new A.Transform2D(
                            new A.Offset() { X = 0, Y = 0 },
                            new A.Extents() { Cx = 12192000, Cy = 6858000 }
                        ),
                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                    );
                    watermarkShape.ShapeProperties.Append(new A.NoFill());
                    watermarkShape.ShapeProperties.Append(new A.Outline(new A.NoFill()));

                    watermarkShape.TextBody = new TextBody(
                        new A.BodyProperties(), new A.ListStyle(),
                        new A.Paragraph(
                            new A.Run(
                                new A.RunProperties() { Language = "en-US", FontSize = fontSize * 100, Bold = true, Dirty = false },
                                new A.Text() { Text = watermarkText }
                            ),
                            new A.EndParagraphRunProperties() { Language = "en-US", Dirty = false }
                        )
                    );

                    var textRun = watermarkShape.TextBody.GetFirstChild<A.Paragraph>()?.GetFirstChild<A.Run>();
                    if (textRun == null) continue;

                    var runProperties = textRun.GetFirstChild<A.RunProperties>();
                    if (runProperties == null) continue;



                    var solidFill = new A.SolidFill(new A.SchemeColor(new A.Alpha() { Val = 25000 }) { Val = A.SchemeColorValues.Accent1 });
                    runProperties.Append(solidFill);

                    var shapeProperties = watermarkShape.ShapeProperties;
                    var transform = shapeProperties.GetFirstChild<A.Transform2D>();
                    if (transform != null)
                    {
                        transform.Rotation = -2700000;
                    }

                    shapeTree.InsertAt(watermarkShape, 0);
                    slideMasterPart.SlideMaster.Save();
                }
            }
        }

        private void AddWatermarkToDocxHeader(string filePath, string watermarkText, int fontSize)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(filePath, true))
            {
                // 获取主文档部分
                MainDocumentPart? mainPart = wordDocument.MainDocumentPart;
                if (mainPart == null) return;

                // 查找或创建第一个节
                var sectionProperties = mainPart.Document.Descendants<SectionProperties>().FirstOrDefault();
                if (sectionProperties == null)
                {
                    // 如果没有节，创建一个
                    sectionProperties = new SectionProperties();
                    mainPart.Document.Append(sectionProperties);
                }

                // 检查是否已有页眉引用
                var existingHeaderRef = sectionProperties.Descendants<HeaderReference>().FirstOrDefault();

                if (existingHeaderRef != null)
                {
                    // 如果已有页眉，获取并修改它
                    var headerPart = mainPart.GetPartById(existingHeaderRef.Id) as HeaderPart;
                    if (headerPart != null)
                    {
                        UpdateHeaderWithWatermark(headerPart.Header, watermarkText, fontSize);
                        headerPart.Header = headerPart.Header;
                    }
                }
                else
                {
                    // 创建新的HeaderPart
                    HeaderPart headerPart = mainPart.AddNewPart<HeaderPart>();

                    // 创建水印内容
                    Header header = new Header();
                    CreateWatermarkParagraph(header, watermarkText, fontSize);

                    headerPart.Header = header;

                    // 添加HeaderReference到sectionProperties
                    string headerRelId = mainPart.GetIdOfPart(headerPart);
                    HeaderReference headerRef = new HeaderReference() { Id = headerRelId, Type = HeaderFooterValues.Default };
                    sectionProperties.PrependChild(headerRef);
                }
            }
        }

        private void AddWatermarkToDocxBody(string filePath, string watermarkText, int fontSize)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(filePath, true))
            {
                MainDocumentPart? mainPart = wordDocument.MainDocumentPart;
                if (mainPart == null) return;

                DocumentFormat.OpenXml.Wordprocessing.Body? body = mainPart.Document.Body;
                if (body == null) return;

                string trimmedText = watermarkText.Trim();
                if (string.IsNullOrEmpty(trimmedText)) return;

                // 创建段落 - 简单实现
                DocumentFormat.OpenXml.Wordprocessing.Paragraph para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();

                // 段落属性 - 居中对齐
                DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties paraProps = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.Justification() { Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center }
                );
                para.Append(paraProps);

                // 运行属性 - 设置字体、大小和浅灰色
                DocumentFormat.OpenXml.Wordprocessing.RunProperties runProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "SimSun", EastAsia = "SimSun" },
                    new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = (fontSize * 2).ToString() },
                    new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "DDDDDD" }
                );

                DocumentFormat.OpenXml.Wordprocessing.Run run = new DocumentFormat.OpenXml.Wordprocessing.Run();
                run.Append(runProps);
                run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(trimmedText));
                para.Append(run);

                // 添加到文档末尾（不遮挡正文）
                body.Append(para);
            }
        }

        private void CreateWatermarkParagraph(Header header, string watermarkText, int fontSize)
        {
            // 创建水印段落 - 居中对齐
            Paragraph watermarkPara = new Paragraph();
            ParagraphProperties paraProps = new ParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            );
            watermarkPara.Append(paraProps);

            Run watermarkRun = new Run();
            watermarkRun.Append(new RunProperties(
                new RunFonts() { Ascii = "SimSun" },
                new FontSize() { Val = (fontSize * 2).ToString() },
                new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "CCCCCC" }
            ));
            watermarkRun.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(watermarkText));
            watermarkPara.Append(watermarkRun);

            header.Append(watermarkPara);
        }

        private void UpdateHeaderWithWatermark(Header header, string watermarkText, int fontSize)
        {
            // 清空现有内容并添加水印
            header.RemoveAllChildren<Paragraph>();
            CreateWatermarkParagraph(header, watermarkText, fontSize);
        }
    }
}