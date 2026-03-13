1. 编译项目
在项目目录下运行：


dotnet build
这会生成可执行文件到 bin\Debug\net8.0-windows\ 目录。

2. 发布为独立可执行文件（推荐）
如果你想生成一个独立的 exe 文件，可以运行：


dotnet publish -c Release -r win-x64 --self-contained true
发布后的文件在 bin\Release\net8.0-windows\win-x64\publish\ 目录下。

3. 运行程序
方式一：直接运行编译输出


dotnet run
方式二：运行生成的exe文件

Debug版本：bin\Debug\net8.0-windows\piccPdfTool.exe
Release版本：bin\Release\net8.0-windows\win-x64\publish\piccPdfTool.exe
4. 使用方法
程序界面现在有两个文件选择按钮：

"选择PPT文件" - 为PPT添加水印（原有功能）
"选择Word文件" - 为Word文档添加水印（新增功能）
输入水印（用英文逗号分隔多个水印），设置字体大小，点击"开始处理"即可在原文件目录下生成带水印的副本。