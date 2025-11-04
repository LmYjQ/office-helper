import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from PIL import Image, ImageTk
import PyPDF2
import os
import io
import tempfile

class PDFSplitterApp:
    def __init__(self, root):
        self.root = root
        self.root.title("PDF分割工具")
        self.root.geometry("900x700")
        
        self.pdf_path = None
        self.pdf_reader = None
        self.total_pages = 0
        self.current_page = 1
        self.page_images = []
        self.pdf_data = None  # 存储PDF文件的二进制数据
        
        self.create_widgets()
        
    def create_widgets(self):
        # 顶部按钮区域
        button_frame = ttk.Frame(self.root)
        button_frame.pack(pady=10)
        
        ttk.Button(button_frame, text="打开PDF", command=self.open_pdf).pack(side=tk.LEFT, padx=5)
        ttk.Button(button_frame, text="保存分割", command=self.save_splits).pack(side=tk.LEFT, padx=5)
        
        # 主内容区域
        main_frame = ttk.Frame(self.root)
        main_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        
        # PDF预览区域
        preview_frame = ttk.LabelFrame(main_frame, text="PDF预览")
        preview_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(0, 5))
        
        self.canvas = tk.Canvas(preview_frame, bg="white")
        self.canvas.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        
        # 页面导航
        nav_frame = ttk.Frame(preview_frame)
        nav_frame.pack(fill=tk.X, pady=5)
        
        ttk.Button(nav_frame, text="上一页", command=self.prev_page).pack(side=tk.LEFT, padx=5)
        ttk.Label(nav_frame, text="页码:").pack(side=tk.LEFT, padx=5)
        self.page_var = tk.StringVar()
        self.page_entry = ttk.Entry(nav_frame, textvariable=self.page_var, width=5)
        self.page_entry.pack(side=tk.LEFT, padx=5)
        self.page_label = ttk.Label(nav_frame, text="/ 0")
        self.page_label.pack(side=tk.LEFT, padx=5)
        ttk.Button(nav_frame, text="跳转", command=self.go_to_page).pack(side=tk.LEFT, padx=5)
        ttk.Button(nav_frame, text="下一页", command=self.next_page).pack(side=tk.LEFT, padx=5)
        
        # 分割设置区域
        split_frame = ttk.LabelFrame(main_frame, text="分割设置")
        split_frame.pack(side=tk.RIGHT, fill=tk.BOTH, padx=(5, 0))
        
        # 输入区域
        input_frame = ttk.Frame(split_frame)
        input_frame.pack(fill=tk.X, pady=5)
        
        ttk.Label(input_frame, text="页码范围 (如: 1-5,6-20):").pack(anchor=tk.W)
        self.range_entry = ttk.Entry(input_frame)
        self.range_entry.pack(fill=tk.X, pady=2)
        
        ttk.Label(input_frame, text="文件名:").pack(anchor=tk.W)
        self.filename_entry = ttk.Entry(input_frame)
        self.filename_entry.pack(fill=tk.X, pady=2)
        
        ttk.Button(input_frame, text="添加分割", command=self.add_split).pack(pady=5)
        
        # 分割列表
        list_frame = ttk.Frame(split_frame)
        list_frame.pack(fill=tk.BOTH, expand=True, pady=5)
        
        columns = ("range", "filename")
        self.split_tree = ttk.Treeview(list_frame, columns=columns, show="headings")
        self.split_tree.heading("range", text="页码范围")
        self.split_tree.heading("filename", text="文件名")
        
        vsb = ttk.Scrollbar(list_frame, orient="vertical", command=self.split_tree.yview)
        hsb = ttk.Scrollbar(list_frame, orient="horizontal", command=self.split_tree.xview)
        self.split_tree.configure(yscrollcommand=vsb.set, xscrollcommand=hsb.set)
        
        self.split_tree.grid(row=0, column=0, sticky="nsew")
        vsb.grid(row=0, column=1, sticky="ns")
        hsb.grid(row=1, column=0, sticky="ew")
        
        list_frame.grid_rowconfigure(0, weight=1)
        list_frame.grid_columnconfigure(0, weight=1)
        
        # 操作按钮
        op_frame = ttk.Frame(split_frame)
        op_frame.pack(fill=tk.X, pady=5)
        
        ttk.Button(op_frame, text="删除选中", command=self.delete_selected).pack(side=tk.LEFT, padx=5)
        ttk.Button(op_frame, text="清空列表", command=self.clear_list).pack(side=tk.LEFT, padx=5)
        
    def open_pdf(self):
        file_path = filedialog.askopenfilename(
            title="选择PDF文件",
            filetypes=[("PDF文件", "*.pdf")]
        )
        
        if file_path:
            try:
                self.pdf_path = file_path
                
                # 读取PDF文件内容到内存
                with open(self.pdf_path, 'rb') as file:
                    self.pdf_data = file.read()
                
                # 使用内存中的PDF数据创建PDF阅读器
                self.pdf_reader = PyPDF2.PdfReader(io.BytesIO(self.pdf_data))
                self.total_pages = len(self.pdf_reader.pages)
                
                # 更新页面显示
                self.page_var.set("1")
                self.update_page_label()
                self.show_page(1)
                
                # 清空之前的列表
                self.clear_list()
                
            except Exception as e:
                messagebox.showerror("错误", f"无法打开PDF文件: {str(e)}")
    
    def show_page(self, page_num):
        if not self.pdf_reader or page_num < 1 or page_num > self.total_pages:
            return
            
        self.current_page = page_num
        self.page_var.set(str(page_num))
        
        # 获取页面
        page = self.pdf_reader.pages[page_num - 1]
        
        try:
            # 尝试使用pdf2image转换页面为图像
            from pdf2image import convert_from_bytes
            
            # 创建一个只包含当前页面的临时PDF
            writer = PyPDF2.PdfWriter()
            writer.add_page(page)
            
            with io.BytesIO() as temp_pdf:
                writer.write(temp_pdf)
                temp_pdf.seek(0)
                images = convert_from_bytes(temp_pdf.read(), first_page=1, last_page=1)
                
                if images:
                    image = images[0]
                else:
                    raise Exception("无法转换页面为图像")
                    
        except ImportError:
            # 如果没有安装pdf2image，使用简单的空白图像
            image = Image.new('RGB', (400, 600), color='white')
        except Exception as e:
            # 其他错误，使用空白图像
            print(f"页面渲染错误: {e}")
            image = Image.new('RGB', (400, 600), color='white')
        
        # 调整图像大小以适应画布
        canvas_width = self.canvas.winfo_width()
        canvas_height = self.canvas.winfo_height()
        
        if canvas_width > 1 and canvas_height > 1:
            image_ratio = image.width / image.height
            canvas_ratio = canvas_width / canvas_height
            
            if image_ratio > canvas_ratio:
                new_width = canvas_width
                new_height = int(canvas_width / image_ratio)
            else:
                new_height = canvas_height
                new_width = int(canvas_height * image_ratio)
                
            image = image.resize((new_width, new_height), Image.LANCZOS)
        
        self.page_image = ImageTk.PhotoImage(image)
        self.canvas.delete("all")
        self.canvas.create_image(
            canvas_width // 2, 
            canvas_height // 2, 
            image=self.page_image, 
            anchor=tk.CENTER
        )
    
    def update_page_label(self):
        self.page_label.config(text=f"/ {self.total_pages}")
    
    def prev_page(self):
        if self.current_page > 1:
            self.show_page(self.current_page - 1)
    
    def next_page(self):
        if self.current_page < self.total_pages:
            self.show_page(self.current_page + 1)
    
    def go_to_page(self):
        try:
            page_num = int(self.page_var.get())
            if 1 <= page_num <= self.total_pages:
                self.show_page(page_num)
            else:
                messagebox.showerror("错误", f"页码必须在1到{self.total_pages}之间")
        except ValueError:
            messagebox.showerror("错误", "请输入有效的页码数字")
    
    def add_split(self):
        range_text = self.range_entry.get().strip()
        filename = self.filename_entry.get().strip()
        
        if not range_text or not filename:
            messagebox.showerror("错误", "请填写完整的页码范围和新文件名")
            return
        
        # 验证页码范围格式
        ranges = range_text.split(',')
        valid_ranges = []
        
        for r in ranges:
            if '-' in r:
                parts = r.split('-')
                if len(parts) == 2:
                    try:
                        start = int(parts[0].strip())
                        end = int(parts[1].strip())
                        if 1 <= start <= end <= self.total_pages:
                            valid_ranges.append((start, end))
                        else:
                            messagebox.showerror("错误", f"页码范围 {r} 无效")
                            return
                    except ValueError:
                        messagebox.showerror("错误", f"页码范围 {r} 格式错误")
                        return
                else:
                    messagebox.showerror("错误", f"页码范围 {r} 格式错误")
                    return
            else:
                try:
                    page = int(r.strip())
                    if 1 <= page <= self.total_pages:
                        valid_ranges.append((page, page))
                    else:
                        messagebox.showerror("错误", f"页码 {r} 无效")
                        return
                except ValueError:
                    messagebox.showerror("错误", f"页码 {r} 格式错误")
                    return
        
        # 添加到列表
        for start, end in valid_ranges:
            if start == end:
                range_display = f"{start}"
            else:
                range_display = f"{start}-{end}"
            self.split_tree.insert("", "end", values=(range_display, filename))
        
        # 清空输入框
        self.range_entry.delete(0, tk.END)
        self.filename_entry.delete(0, tk.END)
    
    def delete_selected(self):
        selected_items = self.split_tree.selection()
        for item in selected_items:
            self.split_tree.delete(item)
    
    def clear_list(self):
        for item in self.split_tree.get_children():
            self.split_tree.delete(item)
    
    def save_splits(self):
        if not self.pdf_path:
            messagebox.showerror("错误", "请先打开PDF文件")
            return
        
        if not self.split_tree.get_children():
            messagebox.showerror("错误", "请添加至少一个分割范围")
            return
        
        # 选择保存目录
        save_dir = filedialog.askdirectory(title="选择保存目录")
        if not save_dir:
            return
        
        try:
            # 重新从内存数据创建PDF阅读器，确保文件状态正确
            pdf_reader = PyPDF2.PdfReader(io.BytesIO(self.pdf_data))
            
            # 处理每个分割范围
            for item in self.split_tree.get_children():
                values = self.split_tree.item(item, "values")
                range_str, filename = values
                
                # 解析页码范围
                if '-' in range_str:
                    start, end = map(int, range_str.split('-'))
                else:
                    start = end = int(range_str)
                
                # 创建PDF写入器
                pdf_writer = PyPDF2.PdfWriter()
                
                # 添加指定页面
                for page_num in range(start, end + 1):
                    page = pdf_reader.pages[page_num - 1]
                    pdf_writer.add_page(page)
                
                # 确保文件名以.pdf结尾
                if not filename.lower().endswith('.pdf'):
                    filename += '.pdf'
                
                # 保存文件
                output_path = os.path.join(save_dir, filename)
                with open(output_path, 'wb') as output_file:
                    pdf_writer.write(output_file)
            
            messagebox.showinfo("成功", "PDF分割完成！")
            
        except Exception as e:
            messagebox.showerror("错误", f"保存过程中发生错误: {str(e)}")

if __name__ == "__main__":
    root = tk.Tk()
    app = PDFSplitterApp(root)
    root.mainloop()