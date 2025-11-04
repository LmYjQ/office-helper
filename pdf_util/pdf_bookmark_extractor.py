#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PDF书签目录提取工具
从PDF文件中提取书签信息并生成目录结构
"""

import os
import sys
import argparse
from typing import List, Dict, Any, Optional

try:
    import pypdf
    from pypdf import PdfReader
except ImportError:
    try:
        import PyPDF2
        from PyPDF2 import PdfReader
    except ImportError:
        print("错误: 需要安装 pypdf 或 PyPDF2 库")
        print("请运行: pip install pypdf")
        sys.exit(1)


class BookmarkExtractor:
    """PDF书签提取器"""

    def __init__(self, pdf_path: str):
        """
        初始化书签提取器

        Args:
            pdf_path: PDF文件路径
        """
        self.pdf_path = pdf_path
        self.reader = None
        self.file_handle = None
        self.bookmarks = []

    def open_pdf(self) -> bool:
        """打开PDF文件"""
        try:
            self.file_handle = open(self.pdf_path, 'rb')
            self.reader = PdfReader(self.file_handle)
            return True
        except Exception as e:
            print(f"无法打开PDF文件: {e}")
            return False

    def close_pdf(self):
        """关闭PDF文件"""
        if self.file_handle:
            self.file_handle.close()
            self.file_handle = None
        self.reader = None

    def extract_bookmarks(self) -> List[Dict[str, Any]]:
        """
        提取书签信息

        Returns:
            书签列表，每个书签包含标题、页码、层级等信息
        """
        if not self.reader:
            return []

        self.bookmarks = []
        self.hierarchical_bookmarks = []  # 保存层级结构

        # 预先创建页面对象映射，提高查找效率
        self._create_page_mapping()

        try:
            # 获取PDF大纲（书签）
            if hasattr(self.reader, 'outline'):
                outline = self.reader.outline
                if outline:
                    self._process_outline_items(outline, level=0)
                    # 扁平化书签列表
                    self._flatten_bookmarks()
                else:
                    print("该PDF文件没有书签")
            else:
                # 尝试使用其他方法获取书签
                self._try_extract_bookmarks_alternative()

        except Exception as e:
            print(f"提取书签时出错: {e}")
            # 尝试备用方法
            self._try_extract_bookmarks_alternative()

        return self.bookmarks

    def _create_page_mapping(self):
        """创建页面对象映射，用于快速查找页码"""
        self.page_mapping = {}

        for i, page in enumerate(self.reader.pages):
            if hasattr(page, 'indirect_reference') and hasattr(page.indirect_reference, 'idnum'):
                # 存储页面对象ID到页码的映射
                self.page_mapping[page.indirect_reference.idnum] = i + 1

                # 存储Contents对象的ID到页码的映射
                try:
                    if '/Contents' in page:
                        contents = page['/Contents']
                        if hasattr(contents, 'idnum'):
                            self.page_mapping[f"contents_{contents.idnum}"] = i + 1
                except:
                    pass

    def _flatten_bookmarks(self):
        """将层级书签结构扁平化为列表"""
        self.bookmarks = []
        for bookmark in self.hierarchical_bookmarks:
            self._add_bookmark_to_flat_list(bookmark)

    def _add_bookmark_to_flat_list(self, bookmark: Dict[str, Any]):
        """递归添加书签到扁平列表"""
        # 添加当前书签
        self.bookmarks.append({
            'title': bookmark['title'],
            'page': bookmark['page'],
            'level': bookmark['level'],
            'children': []  # 在扁平列表中不需要children
        })

        # 递归添加子书签
        children = bookmark.get('children', [])
        for child in children:
            self._add_bookmark_to_flat_list(child)

    def _try_extract_bookmarks_alternative(self):
        """尝试使用备用方法提取书签"""
        try:
            # 尝试直接访问书签
            if hasattr(self.reader, '_get_outline'):
                outline = self.reader._get_outline()
                if outline:
                    self._process_outline_items(outline, level=0)
                    self._flatten_bookmarks()
                    return
        except:
            pass

        # 尝试获取书签的其他方法
        try:
            if hasattr(self.reader, 'get_outline'):
                outline = self.reader.get_outline()
                if outline:
                    self._process_outline_items(outline, level=0)
                    self._flatten_bookmarks()
                    return
        except:
            pass

        print("该PDF文件没有书签或书签格式不支持")

    def _process_outline_items(self, items, level: int = 0, parent_page: int = 1):
        """
        递归处理大纲项目

        Args:
            items: 大纲项目列表
            level: 层级深度
            parent_page: 父级页码
        """
        if not items:
            return

        for item in items:
            try:
                # 处理不同版本的书签格式
                if isinstance(item, dict):
                    # pypdf 格式
                    title = item.get('/Title', '').strip() if '/Title' in item else ''

                    # 获取页码 - 使用优化的映射方法
                    page_num = self._extract_page_number_from_bookmark(item)

                    bookmark_info = {
                        'title': title,
                        'page': page_num,
                        'level': level,
                        'children': []
                    }

                    # 处理子书签 - 新的简单方法
                    children = []

                    # 首先检查是否有Count字段，表示有子项
                    if '/Count' in item:
                        # 对于pypdf库，子项可能直接在outline的嵌套列表中
                        pass  # 子项会在遍历outline时自动处理

                    # 添加到层级结构
                    self.hierarchical_bookmarks.append(bookmark_info)

                    # 处理同级书签
                    if '/Next' in item:
                        next_item = item['/Next']
                        self._process_outline_items([next_item], level, parent_page)

                elif isinstance(item, list):
                    # 如果item是一个列表，这意味着它是子项列表
                    # 递归处理这个列表中的所有项目
                    self._process_outline_items(item, level + 1, parent_page)

                else:
                    # 尝试处理其他格式
                    self._process_item_generic(item, level, parent_page)

            except Exception as e:
                print(f"处理书签项目时出错: {e}")
                continue

    def _extract_page_number_from_bookmark(self, item: dict) -> int:
        """从书签项中提取页码"""
        try:
            # 方法1: 通过页面对象映射查找
            if '/Page' in item:
                page_dict = item['/Page']

                # 查找Contents对象的ID
                if '/Contents' in page_dict:
                    contents = page_dict['/Contents']
                    if hasattr(contents, 'idnum') and contents.idnum in self.page_mapping:
                        return self.page_mapping[contents.idnum]

                # 尝试查找页面对象的直接ID
                if hasattr(page_dict, 'indirect_reference'):
                    ref_id = page_dict.indirect_reference
                    if hasattr(ref_id, 'idnum') and ref_id.idnum in self.page_mapping:
                        return self.page_mapping[ref_id.idnum]

            # 方法2: 从/A字段获取（传统的跳转目标）
            if '/A' in item:
                action = item['/A']
                if '/D' in action:
                    dest = action['/D']
                    if isinstance(dest, list) and len(dest) > 0:
                        page_ref = dest[0]
                        if hasattr(page_ref, 'idnum') and page_ref.idnum in self.page_mapping:
                            return self.page_mapping[page_ref.idnum]

            # 如果都失败了，返回第1页
            return 1

        except Exception as e:
            # 出错时返回第1页
            return 1

    def _collect_children(self, item, children: List, level: int, parent_page: int):
        """收集子书签"""
        try:
            if isinstance(item, dict):
                title = item.get('/Title', '').strip() if '/Title' in item else ''

                page_num = parent_page
                if '/A' in item:
                    action = item['/A']
                    if '/D' in action:
                        dest = action['/D']
                        if isinstance(dest, list) and len(dest) > 0:
                            page_ref = dest[0]
                            if hasattr(page_ref, 'id'):
                                page_num = page_ref.id + 1

                child_info = {
                    'title': title,
                    'page': page_num,
                    'level': level,
                    'children': []
                }

                # 处理子项的子项
                if '/First' in item:
                    self._collect_children(item['/First'], child_info['children'], level + 1, page_num)

                children.append(child_info)

                # 处理同级
                if '/Next' in item:
                    self._collect_children(item['/Next'], children, level, parent_page)

        except Exception as e:
            print(f"收集子书签时出错: {e}")

    def _process_item_generic(self, item, level: int, parent_page: int):
        """处理通用格式的书签项目"""
        try:
            # 尝试获取书签标题
            title = ""
            page_num = parent_page

            if hasattr(item, 'title'):
                title = getattr(item, 'title', '').strip()
            elif hasattr(item, 'get_title'):
                title = item.get_title().strip()
            elif isinstance(item, str):
                title = item

            # 尝试获取页码
            if hasattr(item, 'page'):
                page_num = getattr(item, 'page', parent_page)
            elif hasattr(item, 'dest') and item.dest:
                try:
                    if isinstance(item.dest, list) and len(item.dest) > 0:
                        page_obj = item.dest[0]
                        if hasattr(page_obj, 'id'):
                            page_num = page_obj.id + 1
                except:
                    pass

            if title:  # 只有当标题不为空时才添加
                bookmark_info = {
                    'title': title,
                    'page': page_num,
                    'level': level,
                    'children': []
                }

                self.hierarchical_bookmarks.append(bookmark_info)

                # 处理子项目
                if hasattr(item, 'children') and item.children:
                    for child in item.children:
                        self._process_item_generic(child, level + 1, page_num)

        except Exception as e:
            print(f"处理通用书签项目时出错: {e}")

    def print_bookmarks(self, format_type: str = 'tree'):
        """
        打印书签信息

        Args:
            format_type: 输出格式 ('tree', 'list', 'markdown')
        """
        if not self.bookmarks:
            print("没有找到书签")
            return

        if format_type == 'tree':
            self._print_tree_format()
        elif format_type == 'list':
            self._print_list_format()
        elif format_type == 'markdown':
            self._print_markdown_format()
        else:
            print(f"不支持的格式: {format_type}")

    def _print_tree_format(self):
        """以树形结构打印书签"""
        for bookmark in self.hierarchical_bookmarks:
            self._print_bookmark_tree(bookmark)

    def _print_bookmark_tree(self, bookmark: Dict[str, Any], prefix: str = "", is_last: bool = True):
        """递归打印书签树"""
        # 当前节点
        connector = "└── " if is_last else "├── "
        print(f"{prefix}{connector}{bookmark['title']} (页码: {bookmark['page']})")

        # 子节点前缀
        child_prefix = prefix + ("    " if is_last else "│   ")

        # 打印子节点
        children = bookmark.get('children', [])
        for i, child in enumerate(children):
            is_last_child = (i == len(children) - 1)
            self._print_bookmark_tree(child, child_prefix, is_last_child)

    def _print_list_format(self):
        """以列表格式打印书签"""
        for bookmark in self.bookmarks:
            indent = "  " * bookmark['level']
            print(f"{indent}- {bookmark['title']} (第{bookmark['page']}页)")

    def _print_markdown_format(self):
        """以Markdown格式打印书签"""
        print("# PDF目录\n")
        for bookmark in self.bookmarks:
            indent = "#" * (bookmark['level'] + 1)
            print(f"{indent} {bookmark['title']} (第{bookmark['page']}页)")

    def export_to_file(self, output_path: str, format_type: str = 'markdown'):
        """
        导出书签到文件

        Args:
            output_path: 输出文件路径
            format_type: 输出格式
        """
        try:
            with open(output_path, 'w', encoding='utf-8') as f:
                # 重定向输出到文件
                import io
                from contextlib import redirect_stdout

                f_stdout = io.StringIO()
                with redirect_stdout(f_stdout):
                    self.print_bookmarks(format_type)

                content = f_stdout.getvalue()
                f.write(content)

            print(f"书签已导出到: {output_path}")

        except Exception as e:
            print(f"导出文件时出错: {e}")


def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='PDF书签目录提取工具')
    parser.add_argument('pdf_file', help='PDF文件路径')
    parser.add_argument('-f', '--format', choices=['tree', 'list', 'markdown'],
                       default='tree', help='输出格式 (默认: tree)')
    parser.add_argument('-o', '--output', help='输出文件路径 (可选)')
    parser.add_argument('--encoding', default='utf-8', help='输出文件编码 (默认: utf-8)')

    args = parser.parse_args()

    # 检查文件是否存在
    if not os.path.exists(args.pdf_file):
        print(f"错误: 文件不存在: {args.pdf_file}")
        return

    # 创建提取器
    extractor = BookmarkExtractor(args.pdf_file)

    # 打开PDF文件
    if not extractor.open_pdf():
        return

    try:
        # 提取书签
        bookmarks = extractor.extract_bookmarks()

        if not bookmarks:
            print("该PDF文件没有书签")
            return

        # 输出结果
        if args.output:
            extractor.export_to_file(args.output, args.format)
        else:
            extractor.print_bookmarks(args.format)

    finally:
        # 确保关闭文件
        extractor.close_pdf()


if __name__ == "__main__":
    main()