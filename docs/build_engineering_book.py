"""Build the Iroha Agent engineering handbook as Markdown and DOCX.

The source of truth is engineering_book_content.py. The DOCX deliberately
uses fixed Word geometry so the acceptance copy renders consistently in Word
and LibreOffice.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_ALIGN_VERTICAL, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import nsdecls, qn
from docx.shared import Inches, Pt, RGBColor

from engineering_book_content import DOCUMENT, SECTIONS


HERE = Path(__file__).resolve().parent
PROJECT_ROOT = HERE.parent
MD_PATH = HERE / "彩叶_Iroha_Agent_工程验收与交接手册.md"
DOCX_PATH = HERE / "彩叶_Iroha_Agent_工程验收与交接手册.docx"

PAGE_WIDTH = Inches(8.5)
PAGE_HEIGHT = Inches(11)
MARGIN = Inches(1)
HEADER_DISTANCE = Inches(0.492)
FOOTER_DISTANCE = Inches(0.492)
CONTENT_WIDTH_DXA = 9360
TABLE_INDENT_DXA = 120

FONT_LATIN = "Calibri"
FONT_CJK = "Microsoft YaHei"
FONT_CODE = "Cascadia Mono"

NAVY = "163A59"
BLUE = "2E74B5"
DARK_BLUE = "1F4D78"
CYAN = "35BFD1"
INK = "18364F"
MUTED = "60778D"
LIGHT_BLUE = "E8F4F8"
TABLE_HEADER = "E8EEF5"
TABLE_BORDER = "B8CBD9"
LIGHT_GRAY = "F4F6F9"
SUCCESS_FILL = "EAF7F2"
WARNING_FILL = "FFF7E3"
RISK_FILL = "FDEDED"
NOTE_FILL = "F2F7FA"
WHITE = "FFFFFF"


def rgb(hex_value: str) -> RGBColor:
    return RGBColor.from_string(hex_value)


def set_run_font(run, name=FONT_LATIN, cjk=FONT_CJK, size=None, color=None,
                 bold=None, italic=None):
    run.font.name = name
    run._element.get_or_add_rPr().rFonts.set(qn("w:ascii"), name)
    run._element.get_or_add_rPr().rFonts.set(qn("w:hAnsi"), name)
    run._element.get_or_add_rPr().rFonts.set(qn("w:eastAsia"), cjk)
    if size is not None:
        run.font.size = Pt(size)
    if color is not None:
        run.font.color.rgb = rgb(color)
    if bold is not None:
        run.bold = bold
    if italic is not None:
        run.italic = italic


def set_style_font(style, name=FONT_LATIN, cjk=FONT_CJK, size=None,
                   color=None, bold=None):
    style.font.name = name
    style._element.get_or_add_rPr().rFonts.set(qn("w:ascii"), name)
    style._element.get_or_add_rPr().rFonts.set(qn("w:hAnsi"), name)
    style._element.get_or_add_rPr().rFonts.set(qn("w:eastAsia"), cjk)
    if size is not None:
        style.font.size = Pt(size)
    if color is not None:
        style.font.color.rgb = rgb(color)
    if bold is not None:
        style.font.bold = bold


def set_cell_shading(cell, fill: str):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)
    shd.set(qn("w:val"), "clear")


def set_paragraph_shading(paragraph, fill: str):
    p_pr = paragraph._p.get_or_add_pPr()
    shd = p_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        p_pr.append(shd)
    shd.set(qn("w:fill"), fill)
    shd.set(qn("w:val"), "clear")


def set_paragraph_border(paragraph, side: str, color: str, size=12, space=6):
    p_pr = paragraph._p.get_or_add_pPr()
    p_bdr = p_pr.find(qn("w:pBdr"))
    if p_bdr is None:
        p_bdr = OxmlElement("w:pBdr")
        p_pr.append(p_bdr)
    edge = p_bdr.find(qn(f"w:{side}"))
    if edge is None:
        edge = OxmlElement(f"w:{side}")
        p_bdr.append(edge)
    edge.set(qn("w:val"), "single")
    edge.set(qn("w:sz"), str(size))
    edge.set(qn("w:space"), str(space))
    edge.set(qn("w:color"), color)


def set_cell_margins(cell, top=80, start=120, bottom=80, end=120):
    tc_pr = cell._tc.get_or_add_tcPr()
    tc_mar = tc_pr.find(qn("w:tcMar"))
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for tag, value in (("top", top), ("start", start),
                       ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{tag}"))
        if node is None:
            node = OxmlElement(f"w:{tag}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_repeat_table_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    header = OxmlElement("w:tblHeader")
    header.set(qn("w:val"), "true")
    tr_pr.append(header)


def set_row_cant_split(row):
    tr_pr = row._tr.get_or_add_trPr()
    cant_split = OxmlElement("w:cantSplit")
    tr_pr.append(cant_split)


def set_table_borders(table, color=TABLE_BORDER, size=5):
    tbl_pr = table._tbl.tblPr
    borders = tbl_pr.find(qn("w:tblBorders"))
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        tbl_pr.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = borders.find(qn(f"w:{edge}"))
        if tag is None:
            tag = OxmlElement(f"w:{edge}")
            borders.append(tag)
        tag.set(qn("w:val"), "single")
        tag.set(qn("w:sz"), str(size))
        tag.set(qn("w:space"), "0")
        tag.set(qn("w:color"), color)


def set_table_geometry(table, widths_dxa):
    total = sum(widths_dxa)
    if total != CONTENT_WIDTH_DXA:
        widths_dxa[-1] += CONTENT_WIDTH_DXA - total

    table.autofit = False
    table.alignment = WD_TABLE_ALIGNMENT.LEFT
    tbl_pr = table._tbl.tblPr

    tbl_w = tbl_pr.find(qn("w:tblW"))
    if tbl_w is None:
        tbl_w = OxmlElement("w:tblW")
        tbl_pr.append(tbl_w)
    tbl_w.set(qn("w:w"), str(CONTENT_WIDTH_DXA))
    tbl_w.set(qn("w:type"), "dxa")

    tbl_ind = tbl_pr.find(qn("w:tblInd"))
    if tbl_ind is None:
        tbl_ind = OxmlElement("w:tblInd")
        tbl_pr.append(tbl_ind)
    tbl_ind.set(qn("w:w"), str(TABLE_INDENT_DXA))
    tbl_ind.set(qn("w:type"), "dxa")

    layout = tbl_pr.find(qn("w:tblLayout"))
    if layout is None:
        layout = OxmlElement("w:tblLayout")
        tbl_pr.append(layout)
    layout.set(qn("w:type"), "fixed")

    grid = table._tbl.tblGrid
    for child in list(grid):
        grid.remove(child)
    for width in widths_dxa:
        col = OxmlElement("w:gridCol")
        col.set(qn("w:w"), str(width))
        grid.append(col)

    for row in table.rows:
        for index, cell in enumerate(row.cells):
            width = widths_dxa[index]
            tc_pr = cell._tc.get_or_add_tcPr()
            tc_w = tc_pr.find(qn("w:tcW"))
            if tc_w is None:
                tc_w = OxmlElement("w:tcW")
                tc_pr.append(tc_w)
            tc_w.set(qn("w:w"), str(width))
            tc_w.set(qn("w:type"), "dxa")
            cell.width = Inches(width / 1440)


def add_field(paragraph, instruction: str):
    run = paragraph.add_run()
    fld_char = OxmlElement("w:fldChar")
    fld_char.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = instruction
    separate = OxmlElement("w:fldChar")
    separate.set(qn("w:fldCharType"), "separate")
    text = OxmlElement("w:t")
    text.text = "1"
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    run._r.extend((fld_char, instr, separate, text, end))
    return run


def add_numbering_definition(doc, marker: str, num_fmt: str, start=1,
                             left=540, hanging=270, font=None):
    numbering = doc.part.numbering_part.element
    abstract_ids = [int(n.get(qn("w:abstractNumId"))) for n in
                    numbering.findall(qn("w:abstractNum"))]
    num_ids = [int(n.get(qn("w:numId"))) for n in numbering.findall(qn("w:num"))]
    abstract_id = max(abstract_ids, default=-1) + 1
    num_id = max(num_ids, default=0) + 1

    abstract = OxmlElement("w:abstractNum")
    abstract.set(qn("w:abstractNumId"), str(abstract_id))
    multi = OxmlElement("w:multiLevelType")
    multi.set(qn("w:val"), "singleLevel")
    abstract.append(multi)

    level = OxmlElement("w:lvl")
    level.set(qn("w:ilvl"), "0")
    start_node = OxmlElement("w:start")
    start_node.set(qn("w:val"), str(start))
    level.append(start_node)
    fmt = OxmlElement("w:numFmt")
    fmt.set(qn("w:val"), num_fmt)
    level.append(fmt)
    text = OxmlElement("w:lvlText")
    text.set(qn("w:val"), marker)
    level.append(text)
    justification = OxmlElement("w:lvlJc")
    justification.set(qn("w:val"), "left")
    level.append(justification)

    p_pr = OxmlElement("w:pPr")
    tabs = OxmlElement("w:tabs")
    tab = OxmlElement("w:tab")
    tab.set(qn("w:val"), "num")
    tab.set(qn("w:pos"), str(left))
    tabs.append(tab)
    p_pr.append(tabs)
    ind = OxmlElement("w:ind")
    ind.set(qn("w:left"), str(left))
    ind.set(qn("w:hanging"), str(hanging))
    p_pr.append(ind)
    spacing = OxmlElement("w:spacing")
    spacing.set(qn("w:after"), "80")
    spacing.set(qn("w:line"), "300")
    spacing.set(qn("w:lineRule"), "auto")
    p_pr.append(spacing)
    level.append(p_pr)

    if font:
        r_pr = OxmlElement("w:rPr")
        r_fonts = OxmlElement("w:rFonts")
        r_fonts.set(qn("w:ascii"), font)
        r_fonts.set(qn("w:hAnsi"), font)
        r_fonts.set(qn("w:eastAsia"), font)
        r_pr.append(r_fonts)
        level.append(r_pr)

    abstract.append(level)
    numbering.append(abstract)

    num = OxmlElement("w:num")
    num.set(qn("w:numId"), str(num_id))
    abstract_num_id = OxmlElement("w:abstractNumId")
    abstract_num_id.set(qn("w:val"), str(abstract_id))
    num.append(abstract_num_id)
    numbering.append(num)
    return num_id


def apply_numbering(paragraph, num_id: int):
    p_pr = paragraph._p.get_or_add_pPr()
    num_pr = p_pr.find(qn("w:numPr"))
    if num_pr is None:
        num_pr = OxmlElement("w:numPr")
        p_pr.append(num_pr)
    ilvl = OxmlElement("w:ilvl")
    ilvl.set(qn("w:val"), "0")
    num = OxmlElement("w:numId")
    num.set(qn("w:val"), str(num_id))
    num_pr.extend((ilvl, num))


def configure_document(doc: Document):
    section = doc.sections[0]
    section.page_width = PAGE_WIDTH
    section.page_height = PAGE_HEIGHT
    section.top_margin = MARGIN
    section.bottom_margin = MARGIN
    section.left_margin = MARGIN
    section.right_margin = MARGIN
    section.header_distance = HEADER_DISTANCE
    section.footer_distance = FOOTER_DISTANCE
    section.different_first_page_header_footer = True

    styles = doc.styles
    normal = styles["Normal"]
    set_style_font(normal, size=11, color=INK)
    normal.paragraph_format.space_before = Pt(0)
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.25
    normal.paragraph_format.widow_control = True

    title = styles["Title"]
    set_style_font(title, size=29, color=NAVY, bold=True)
    title.paragraph_format.space_before = Pt(0)
    title.paragraph_format.space_after = Pt(8)

    subtitle = styles["Subtitle"]
    set_style_font(subtitle, size=14, color=DARK_BLUE)
    subtitle.paragraph_format.space_before = Pt(0)
    subtitle.paragraph_format.space_after = Pt(18)

    h1 = styles["Heading 1"]
    set_style_font(h1, size=16, color=BLUE, bold=True)
    h1.paragraph_format.space_before = Pt(18)
    h1.paragraph_format.space_after = Pt(10)
    h1.paragraph_format.keep_with_next = True
    h1.paragraph_format.keep_together = True

    h2 = styles["Heading 2"]
    set_style_font(h2, size=13, color=BLUE, bold=True)
    h2.paragraph_format.space_before = Pt(14)
    h2.paragraph_format.space_after = Pt(7)
    h2.paragraph_format.keep_with_next = True

    h3 = styles["Heading 3"]
    set_style_font(h3, size=12, color=DARK_BLUE, bold=True)
    h3.paragraph_format.space_before = Pt(10)
    h3.paragraph_format.space_after = Pt(5)
    h3.paragraph_format.keep_with_next = True

    for name, size, color in (
        ("Handbook Kicker", 9.5, CYAN),
        ("Handbook Metadata", 9.5, MUTED),
        ("Handbook TOC", 9.5, DARK_BLUE),
        ("Table Text", 9.0, INK),
        ("Table Text Compact", 8.4, INK),
        ("Code Block", 8.6, INK),
        ("Figure Caption", 8.5, MUTED),
    ):
        if name not in styles:
            style = styles.add_style(name, 1)
        else:
            style = styles[name]
        set_style_font(style, size=size, color=color,
                       name=FONT_CODE if name == "Code Block" else FONT_LATIN,
                       cjk=FONT_CJK)
        style.paragraph_format.space_before = Pt(0)
        style.paragraph_format.space_after = Pt(4)
        style.paragraph_format.line_spacing = 1.15 if "Table" in name else 1.2

    styles["Handbook Kicker"].font.bold = True
    styles["Handbook TOC"].paragraph_format.space_after = Pt(0)
    styles["Handbook TOC"].paragraph_format.line_spacing = 1.05
    styles["Code Block"].paragraph_format.space_before = Pt(5)
    styles["Code Block"].paragraph_format.space_after = Pt(7)
    styles["Code Block"].paragraph_format.left_indent = Inches(0.12)
    styles["Code Block"].paragraph_format.right_indent = Inches(0.12)
    styles["Figure Caption"].paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER

    settings = doc.settings._element
    update_fields = settings.find(qn("w:updateFields"))
    if update_fields is None:
        update_fields = OxmlElement("w:updateFields")
        settings.append(update_fields)
    update_fields.set(qn("w:val"), "true")

    return {
        "bullet": add_numbering_definition(doc, "•", "bullet", left=540, hanging=270),
        "steps": add_numbering_definition(doc, "%1.", "decimal", left=540, hanging=270),
        "check": add_numbering_definition(doc, "□", "bullet", left=540, hanging=270,
                                          font=FONT_CJK),
    }


def configure_headers_and_footers(doc: Document):
    section = doc.sections[0]

    first_header = section.first_page_header
    first_header.is_linked_to_previous = False
    first_header.paragraphs[0].text = ""
    first_footer = section.first_page_footer
    first_footer.is_linked_to_previous = False
    first_footer.paragraphs[0].text = ""

    header = section.header
    header.is_linked_to_previous = False
    hp = header.paragraphs[0]
    hp.text = ""
    hp.alignment = WD_ALIGN_PARAGRAPH.LEFT
    hp.paragraph_format.space_after = Pt(3)
    run = hp.add_run("彩叶 Iroha Agent  |  工程验收与交接手册")
    set_run_font(run, size=8.5, color=MUTED, bold=True)
    set_paragraph_border(hp, "bottom", TABLE_BORDER, size=4, space=3)

    footer = section.footer
    footer.is_linked_to_previous = False
    fp = footer.paragraphs[0]
    fp.text = ""
    fp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    fp.paragraph_format.space_before = Pt(3)
    set_paragraph_border(fp, "top", TABLE_BORDER, size=4, space=3)
    run = fp.add_run("内部工程基线  |  第 ")
    set_run_font(run, size=8, color=MUTED)
    page_run = add_field(fp, "PAGE")
    set_run_font(page_run, size=8, color=MUTED)
    run = fp.add_run(" 页")
    set_run_font(run, size=8, color=MUTED)


def add_cover(doc: Document):
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_after = Pt(44)

    kicker = doc.add_paragraph(style="Handbook Kicker")
    kicker.alignment = WD_ALIGN_PARAGRAPH.CENTER
    kicker.add_run("ENGINEERING ACCEPTANCE HANDBOOK")

    title = doc.add_paragraph(style="Title")
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title.add_run(DOCUMENT["title"])

    subtitle = doc.add_paragraph(style="Subtitle")
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    subtitle.add_run(DOCUMENT["subtitle"])

    rule = doc.add_paragraph()
    rule.paragraph_format.space_before = Pt(0)
    rule.paragraph_format.space_after = Pt(16)
    set_paragraph_border(rule, "bottom", CYAN, size=10, space=1)

    evidence = HERE / DOCUMENT["evidence_image"]
    if evidence.exists():
        image_p = doc.add_paragraph()
        image_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        image_p.paragraph_format.space_after = Pt(5)
        run = image_p.add_run()
        shape = run.add_picture(str(evidence), width=Inches(6.25))
        doc_pr = shape._inline.docPr
        doc_pr.set("descr", "Windows 主版本独立发布包 1280 x 720 验收截图")
        caption = doc.add_paragraph(style="Figure Caption")
        caption.add_run("图 1  Windows 主版本独立发布包视觉基线（1280 x 720）")

    meta = doc.add_paragraph(style="Handbook Metadata")
    meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta.paragraph_format.space_before = Pt(10)
    meta.paragraph_format.space_after = Pt(3)
    run = meta.add_run(f"{DOCUMENT['version']}  |  基线日期 {DOCUMENT['baseline_date']}")
    set_run_font(run, size=10, color=DARK_BLUE, bold=True)

    meta2 = doc.add_paragraph(style="Handbook Metadata")
    meta2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta2.add_run(DOCUMENT["baseline"])

    status = doc.add_paragraph()
    status.alignment = WD_ALIGN_PARAGRAPH.CENTER
    status.paragraph_format.space_before = Pt(7)
    status.paragraph_format.space_after = Pt(0)
    status.paragraph_format.left_indent = Inches(0.55)
    status.paragraph_format.right_indent = Inches(0.55)
    set_paragraph_shading(status, SUCCESS_FILL)
    set_paragraph_border(status, "left", CYAN, size=14, space=8)
    run = status.add_run("验收建议：" + DOCUMENT["status"])
    set_run_font(run, size=10, color=NAVY, bold=True)

    doc.add_page_break()


def add_navigation(doc: Document):
    heading = doc.add_paragraph("阅读导航", style="Heading 1")
    heading.paragraph_format.space_before = Pt(0)
    p = doc.add_paragraph(
        "本手册把设计事实、工程事实和验收判断分开记录。按角色选择阅读路径，"
        "再使用后续章节中的检查表和偏差记录完成签字。"
    )
    p.paragraph_format.space_after = Pt(9)

    for label, text, tone in (
        ("验收负责人", "先读第 1、10、11、12、19 章，执行现场复验并记录偏差。", "success"),
        ("维护工程师", "先读第 3、5、6、7、8、13、15 章，再按第 17 章发布。", "info"),
        ("后续开发者", "先读第 3、4、15、16、18 章，扩展前先拆分单文件边界。", "warning"),
    ):
        add_callout(doc, label, text, tone)

    toc_heading = doc.add_paragraph("章节目录", style="Heading 2")
    toc_heading.paragraph_format.space_before = Pt(12)
    for section in SECTIONS:
        p = doc.add_paragraph(style="Handbook TOC")
        p.paragraph_format.left_indent = Inches(0.15)
        run = p.add_run(section["title"])
        set_run_font(run, size=9.0, color=DARK_BLUE,
                     bold=section["title"].startswith(("附录", "文档")))
    doc.add_page_break()


def add_callout(doc, label: str, text: str, tone="info"):
    fills = {
        "success": SUCCESS_FILL,
        "warning": WARNING_FILL,
        "risk": RISK_FILL,
        "note": NOTE_FILL,
        "info": LIGHT_BLUE,
    }
    accents = {
        "success": "2EA57B",
        "warning": "D6A12E",
        "risk": "C85858",
        "note": "7597AE",
        "info": CYAN,
    }
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(5)
    p.paragraph_format.space_after = Pt(8)
    p.paragraph_format.left_indent = Inches(0.09)
    p.paragraph_format.right_indent = Inches(0.05)
    p.paragraph_format.line_spacing = 1.18
    set_paragraph_shading(p, fills.get(tone, NOTE_FILL))
    set_paragraph_border(p, "left", accents.get(tone, CYAN), size=16, space=7)
    label_run = p.add_run(label + "  ")
    set_run_font(label_run, size=10.5, color=NAVY, bold=True)
    text_run = p.add_run(text)
    set_run_font(text_run, size=10.3, color=INK)
    return p


def add_body_paragraph(doc, text: str):
    p = doc.add_paragraph()
    p.paragraph_format.keep_together = False
    run = p.add_run(text)
    set_run_font(run, size=11, color=INK)
    return p


def width_values(widths):
    values = [int(round(float(value) * 1440)) for value in widths]
    values[-1] += CONTENT_WIDTH_DXA - sum(values)
    return values


def add_table(doc, block):
    headers = block["headers"]
    rows = block["rows"]
    compact = block.get("compact", False)
    dense = block.get("dense", False)
    compact_margins = {"top": 30, "bottom": 30} if dense else ({"top": 60, "bottom": 60} if compact else {})
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.LEFT
    table.autofit = False
    table.style = "Table Grid"
    table.rows[0].cells[0]

    for index, header in enumerate(headers):
        cell = table.rows[0].cells[index]
        cell.text = ""
        cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
        set_cell_shading(cell, TABLE_HEADER)
        set_cell_margins(cell, **compact_margins)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_after = Pt(0)
        p.paragraph_format.line_spacing = 1.1
        run = p.add_run(str(header))
        set_run_font(run, size=8.4 if dense else (8.7 if compact else 9.2), color=NAVY, bold=True)

    for row_values in rows:
        row = table.add_row()
        set_row_cant_split(row)
        for index, value in enumerate(row_values):
            cell = row.cells[index]
            cell.text = ""
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            set_cell_margins(cell, **compact_margins)
            p = cell.paragraphs[0]
            p.style = "Table Text Compact" if compact else "Table Text"
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.keep_together = True
            if len(str(value)) <= 8 and index > 0:
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            run = p.add_run(str(value))
            set_run_font(run, size=8.0 if dense else (8.4 if compact else 9.0), color=INK)

    set_repeat_table_header(table.rows[0])
    set_table_borders(table)
    set_table_geometry(table, width_values(block["widths"]))

    after = doc.add_paragraph()
    after.paragraph_format.space_after = Pt(2)
    after.paragraph_format.line_spacing = 0.2
    return table


def add_list(doc, items, num_id):
    for item in items:
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.line_spacing = 1.25
        p.paragraph_format.widow_control = True
        apply_numbering(p, num_id)
        run = p.add_run(item)
        set_run_font(run, size=10.7, color=INK)


def add_code(doc, text: str):
    p = doc.add_paragraph(style="Code Block")
    set_paragraph_shading(p, LIGHT_GRAY)
    set_paragraph_border(p, "left", "9BB9CC", size=10, space=7)
    lines = text.splitlines() or [""]
    for index, line in enumerate(lines):
        if index:
            p.add_run().add_break()
        run = p.add_run(line)
        set_run_font(run, name=FONT_CODE, cjk=FONT_CJK, size=8.6, color=INK)
    return p


def add_section(doc, section, numbering, page_break=False):
    if page_break:
        doc.add_page_break()
    heading = doc.add_paragraph(section["title"], style=f"Heading {section.get('level', 1)}")
    heading.paragraph_format.space_before = Pt(0)
    set_paragraph_border(heading, "bottom", "CDE5ED", size=5, space=4)

    for block in section.get("blocks", []):
        kind = block["type"]
        if kind == "p":
            add_body_paragraph(doc, block["text"])
        elif kind in ("lead", "note"):
            add_callout(doc, block["label"], block["text"], block.get("tone", "info"))
        elif kind == "table":
            add_table(doc, block)
        elif kind == "bullets":
            add_list(doc, block["items"], numbering["bullet"])
        elif kind == "steps":
            add_list(doc, block["items"], numbering["steps"])
        elif kind == "checklist":
            add_list(doc, block["items"], numbering["check"])
        elif kind == "code":
            add_code(doc, block["text"])
        else:
            raise ValueError(f"Unsupported block type: {kind}")


def md_escape(value) -> str:
    return str(value).replace("|", "\\|").replace("\n", "<br>")


def block_to_markdown(block) -> list[str]:
    kind = block["type"]
    if kind == "p":
        return [block["text"], ""]
    if kind in ("lead", "note"):
        return [f"> **{block['label']}**  {block['text']}", ""]
    if kind == "bullets":
        return [*(f"- {item}" for item in block["items"]), ""]
    if kind == "steps":
        return [*(f"{index}. {item}" for index, item in enumerate(block["items"], 1)), ""]
    if kind == "checklist":
        return [*(f"- [ ] {item}" for item in block["items"]), ""]
    if kind == "code":
        return ["```text", block["text"], "```", ""]
    if kind == "table":
        headers = block["headers"]
        lines = ["| " + " | ".join(md_escape(x) for x in headers) + " |"]
        lines.append("| " + " | ".join("---" for _ in headers) + " |")
        for row in block["rows"]:
            lines.append("| " + " | ".join(md_escape(x) for x in row) + " |")
        lines.append("")
        return lines
    raise ValueError(kind)


def build_markdown():
    lines = [
        f"# {DOCUMENT['title']}",
        "",
        f"## {DOCUMENT['subtitle']}",
        "",
        f"- 文档版本：{DOCUMENT['version']}",
        f"- 软件基线日期：{DOCUMENT['baseline_date']}",
        f"- 软件基线：{DOCUMENT['baseline']}",
        f"- 建议结论：{DOCUMENT['status']}",
        "",
        f"![Windows 主版本独立发布包视觉基线]({DOCUMENT['evidence_image']})",
        "",
        "## 章节目录",
        "",
    ]
    lines.extend(f"- {section['title']}" for section in SECTIONS)
    lines.append("")

    for section in SECTIONS:
        level = min(max(int(section.get("level", 1)) + 1, 2), 6)
        lines.extend(["#" * level + " " + section["title"], ""])
        for block in section.get("blocks", []):
            lines.extend(block_to_markdown(block))
    MD_PATH.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def scrub_core_properties(doc: Document):
    props = doc.core_properties
    props.author = "Iroha Agent Project"
    props.last_modified_by = "Iroha Agent Project"
    props.title = f"{DOCUMENT['title']} {DOCUMENT['subtitle']}"
    props.subject = "工程验收、核查、扩展与交接"
    props.keywords = "Iroha Agent, 验收, 工程交接, Windows, Android, DeepSeek, GPT-SoVITS"
    props.comments = "Generated from the project engineering baseline. Contains no API keys."


def build_docx():
    doc = Document()
    numbering = configure_document(doc)
    configure_headers_and_footers(doc)
    scrub_core_properties(doc)
    add_cover(doc)
    add_navigation(doc)
    flow_on_previous_page = {
        "8. 配置、数据与保留策略",
        "12. 当前实测证据",
        "17. 运维、发布与变更管理",
    }
    for index, section in enumerate(SECTIONS):
        add_section(
            doc,
            section,
            numbering,
            page_break=index > 0 and section["title"] not in flow_on_previous_page,
        )
    doc.save(DOCX_PATH)


def main():
    build_markdown()
    build_docx()
    print(MD_PATH)
    print(DOCX_PATH)


if __name__ == "__main__":
    main()
