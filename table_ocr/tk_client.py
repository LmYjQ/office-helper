import tkinter as tk
from tkinter import filedialog
from gradio_client import Client, handle_file
import os
import csv


def parse_ocr_result(result_file_path, csv_output_path="parsed_result.csv"):
    """
    解析 OCR 结果文件并生成 CSV 表格
    :param result_file_path: OCR 结果文件路径（如 1.jpg_result.txt）
    :param csv_output_path: 生成的 CSV 文件路径
    """
    # 确保输出目录存在
    os.makedirs(os.path.dirname(csv_output_path), exist_ok=True)

    # 读取 OCR 结果文件
    with open(result_file_path, "r", encoding="gbk") as f:
        result_content = f.read()

    # 结果是一个只有一行的字符串，去掉前两个和最后两个字符，然后先用<nl>分割成多行，再用<fcel>把每一行分割成多列，输出一个csv文件
    result_content = result_content[2:-2]
    result_content = result_content.replace("<nl>", "\n")
    result_content = result_content.replace("<fcel>", ",")
    
    # 写入 CSV 文件
    with open(csv_output_path, "w", newline='', encoding="utf-8") as csvfile:
        csv_writer = csv.writer(csvfile)
        for line in result_content.splitlines():
            row = line.split(",")
            csv_writer.writerow(row)

    print(f"CSV 表格已生成：{csv_output_path}")


# 合并结果按钮
def merge_csv():
    def _merge_csv(folder_path):
        input_dir = os.path.join(folder_path, "csv_output")
        if os.path.exists(input_dir):
            # 合并所有 CSV 文件
            output_file = os.path.join(folder_path, "merged.csv")
            with open(output_file, "w", newline='', encoding="utf-8") as merged_file:
                csv_writer = csv.writer(merged_file)
                for filename in os.listdir(input_dir):
                    if filename.endswith(".csv"):
                        file_path = os.path.join(input_dir, filename)
                        with open(file_path, "r", encoding="utf-8") as csv_file:
                            csv_reader = csv.reader(csv_file)
                            for row in csv_reader:
                                csv_writer.writerow(row)
                            csv_writer.writerow([''])
            print(f"合并完成，结果已保存到: {output_file}")
        else:
            print(f"未找到 {input_dir} 文件夹")
    return _merge_csv

def ocr(folder_path, orc_result_path="ocr_result", csv_output_path="csv_output"):
    # 创建结果目录
    os.makedirs(os.path.join(folder_path, orc_result_path), exist_ok=True)
    os.makedirs(os.path.join(folder_path, csv_output_path), exist_ok=True)
    
    client = Client("https://paddlepaddle-paddleocr-vl-online-demo.ms.show/")
    import time
    
    # 获取所有图片文件
    image_files = [f for f in os.listdir(folder_path) if f.endswith(".png") or f.endswith(".jpg")]
    total_images = len(image_files)
    print(f"共发现 {total_images} 张图片需要处理")
    
    for idx, filename in enumerate(image_files, 1):
        file_path = os.path.join(folder_path, filename)
        txt_path = os.path.join(folder_path, orc_result_path, filename.replace('.png', '.txt').replace('.jpg', '.txt'))
        csv_path = os.path.join(folder_path, csv_output_path, filename.replace('.png', '.csv').replace('.jpg', '.csv'))
        # 如果已存在对应的csv文件，则跳过
        if os.path.exists(txt_path):
            print(f"已存在txt文件，跳过: {txt_path}")
            continue
        print(f"正在处理第 {idx}/{total_images} 张图片: {filename}")
        start_time = time.time()
        result = client.predict(
            file_path=handle_file(file_path),
            api_name="/handle_targeted_recognition"
        )
        end_time = time.time()
        print(f"处理图片 {filename} 耗时: {end_time - start_time:.2f} 秒")
        # 写入文本文件
        with open(txt_path, "w") as f:
            f.write(str(result))
        # 解析为 CSV 文件
        parse_ocr_result(txt_path, csv_path)

    print("所有图片处理完成，开始合并结果...")
    merge_csv_func = merge_csv()
    merge_csv_func(folder_path)

# Tkinter GUI
def select_folder():
    folder_path = filedialog.askdirectory()
    if folder_path:
        ocr(folder_path)


root = tk.Tk()
root.title("OCR 客户端")

# URL 配置
url_label = tk.Label(root, text="API URL:")
url_label.pack()
url_entry = tk.Entry(root, width=50)
url_entry.insert(0, "https://paddlepaddle-paddleocr-vl-online-demo.ms.show/")
url_entry.pack()

# 文件夹选择按钮
select_button = tk.Button(root, text="选择文件夹", command=select_folder)
select_button.pack()



# merge_button = tk.Button(root, text="合并结果", command=merge_csv)
# merge_button.pack()

root.mainloop()