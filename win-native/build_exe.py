#!/usr/bin/env python3
"""
Builds a standalone 64-bit Windows GUI executable (release/FreeOverlay.exe)
using clang (-target x86_64-pc-windows-msvc) + COFF-to-PE32+ linker.
"""
import hashlib
import os
import struct
import subprocess

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CS_PATH = os.path.join(ROOT_DIR, "win-native", "FreeOverlayApp.cs")
RELEASE_DIR = os.path.join(ROOT_DIR, "release")
OUT_EXE = os.path.join(RELEASE_DIR, "FreeOverlay.exe")

KERNEL32_IMPORTS = [
    "GetTempPathA",
    "CreateDirectoryA",
    "CreateFileA",
    "WriteFile",
    "CloseHandle",
    "GetFileAttributesA",
    "CreateProcessA",
    "WaitForSingleObject",
    "ExitProcess",
]

DPI_MANIFEST_XML = b"""<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1" xmlns:asmv3="urn:schemas-microsoft-com:asm.v3">
  <asmv3:application>
    <asmv3:windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
    </asmv3:windowsSettings>
  </asmv3:application>
</assembly>
"""


def align_up(val: int, align: int) -> int:
    return (val + align - 1) & ~(align - 1)


def generate_c_launcher(cs_bytes: bytes) -> str:
    hex_bytes = ", ".join(f"0x{b:02x}" for b in cs_bytes)
    manifest_hex = ", ".join(f"0x{b:02x}" for b in DPI_MANIFEST_XML)
    short_hash = hashlib.sha256(cs_bytes + DPI_MANIFEST_XML).hexdigest()[:8]
    return f"""
typedef unsigned int DWORD;
typedef int BOOL;
typedef void* HANDLE;
typedef const char* LPCSTR;
typedef char* LPSTR;

typedef struct _STARTUPINFOA {{
    DWORD cb;
    LPSTR lpReserved;
    LPSTR lpDesktop;
    LPSTR lpTitle;
    DWORD dwX;
    DWORD dwY;
    DWORD dwXSize;
    DWORD dwYSize;
    DWORD dwXCountChars;
    DWORD dwYCountChars;
    DWORD dwFillAttribute;
    DWORD dwFlags;
    unsigned short wShowWindow;
    unsigned short cbReserved2;
    unsigned char* lpReserved2;
    HANDLE hStdInput;
    HANDLE hStdOutput;
    HANDLE hStdError;
}} STARTUPINFOA;

typedef struct _PROCESS_INFORMATION {{
    HANDLE hProcess;
    HANDLE hThread;
    DWORD dwProcessId;
    DWORD dwThreadId;
}} PROCESS_INFORMATION;

__declspec(dllimport) DWORD __stdcall GetTempPathA(DWORD nBufferLength, LPSTR lpBuffer);
__declspec(dllimport) BOOL __stdcall CreateDirectoryA(LPCSTR lpPathName, void* lpSecurityAttributes);
__declspec(dllimport) HANDLE __stdcall CreateFileA(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, void* lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);
__declspec(dllimport) BOOL __stdcall WriteFile(HANDLE hFile, const void* lpBuffer, DWORD nNumberOfBytesToWrite, DWORD* lpNumberOfBytesWritten, void* lpOverlapped);
__declspec(dllimport) BOOL __stdcall CloseHandle(HANDLE hObject);
__declspec(dllimport) DWORD __stdcall GetFileAttributesA(LPCSTR lpFileName);
__declspec(dllimport) BOOL __stdcall CreateProcessA(LPCSTR lpApplicationName, LPSTR lpCommandLine, void* lpProcessAttributes, void* lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, void* lpEnvironment, LPCSTR lpCurrentDirectory, STARTUPINFOA* lpStartupInfo, PROCESS_INFORMATION* lpProcessInformation);
__declspec(dllimport) DWORD __stdcall WaitForSingleObject(HANDLE hHandle, DWORD dwMilliseconds);
__declspec(dllimport) void __stdcall ExitProcess(DWORD uExitCode);

static const unsigned char CS_PAYLOAD[{len(cs_bytes)}] = {{
    {hex_bytes}
}};

static const unsigned char MANIFEST_PAYLOAD[{len(DPI_MANIFEST_XML)}] = {{
    {manifest_hex}
}};

static void copy_str(char* dst, const char* src) {{
    while (*src) {{
        *dst++ = *src++;
    }}
    *dst = 0;
}}

static void append_str(char* dst, const char* src) {{
    while (*dst) dst++;
    copy_str(dst, src);
}}

static void zero_mem(void* ptr, unsigned int size) {{
    unsigned char* p = (unsigned char*)ptr;
    for (unsigned int i = 0; i < size; i++) p[i] = 0;
}}

void __stdcall WinMainCRTStartup(void) {{
    char tempDir[512];
    char workDir[512];
    char csPath[512];
    char manifestPath[512];
    char exePath[512];
    char cmdLine[1500];

    zero_mem(tempDir, sizeof(tempDir));
    DWORD len = GetTempPathA(480, tempDir);
    if (len == 0 || len > 480) {{
        ExitProcess(1);
    }}

    copy_str(workDir, tempDir);
    append_str(workDir, "FreeOverlay_{short_hash}");
    CreateDirectoryA(workDir, (void*)0);

    copy_str(csPath, workDir);
    append_str(csPath, "\\\\FreeOverlayApp.cs");

    copy_str(manifestPath, workDir);
    append_str(manifestPath, "\\\\app.manifest");

    copy_str(exePath, workDir);
    append_str(exePath, "\\\\FreeOverlayGui.exe");

    DWORD attr = GetFileAttributesA(exePath);
    if (attr == 0xFFFFFFFF) {{
        HANDLE hFile = CreateFileA(
            csPath,
            0x40000000,
            0,
            (void*)0,
            2,
            0x80,
            (void*)0
        );
        if (hFile != (HANDLE)-1) {{
            DWORD written = 0;
            WriteFile(hFile, CS_PAYLOAD, {len(cs_bytes)}, &written, (void*)0);
            CloseHandle(hFile);
        }}

        HANDLE hManifest = CreateFileA(
            manifestPath,
            0x40000000,
            0,
            (void*)0,
            2,
            0x80,
            (void*)0
        );
        if (hManifest != (HANDLE)-1) {{
            DWORD writtenM = 0;
            WriteFile(hManifest, MANIFEST_PAYLOAD, {len(DPI_MANIFEST_XML)}, &writtenM, (void*)0);
            CloseHandle(hManifest);
        }}

        // Compile with PerMonitorV2 High-DPI Win32 Manifest embedded
        copy_str(cmdLine, "\\"C:\\\\Windows\\\\Microsoft.NET\\\\Framework64\\\\v4.0.30319\\\\csc.exe\\" /nologo /target:winexe /optimize+ /win32manifest:\\"");
        append_str(cmdLine, manifestPath);
        append_str(cmdLine, "\\" /out:\\"");
        append_str(cmdLine, exePath);
        append_str(cmdLine, "\\" \\"");
        append_str(cmdLine, csPath);
        append_str(cmdLine, "\\"");

        STARTUPINFOA si;
        PROCESS_INFORMATION pi;
        zero_mem(&si, sizeof(si));
        zero_mem(&pi, sizeof(pi));
        si.cb = sizeof(si);

        if (CreateProcessA(
            (LPCSTR)0,
            cmdLine,
            (void*)0,
            (void*)0,
            0,
            0x08000000, // CREATE_NO_WINDOW
            (void*)0,
            workDir,
            &si,
            &pi
        )) {{
            WaitForSingleObject(pi.hProcess, 20000);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }}
    }}

    STARTUPINFOA siGui;
    PROCESS_INFORMATION piGui;
    zero_mem(&siGui, sizeof(siGui));
    zero_mem(&piGui, sizeof(piGui));
    siGui.cb = sizeof(siGui);

    copy_str(cmdLine, "\\"");
    append_str(cmdLine, exePath);
    append_str(cmdLine, "\\"");

    if (CreateProcessA(
        exePath,
        cmdLine,
        (void*)0,
        (void*)0,
        0,
        0,
        (void*)0,
        workDir,
        &siGui,
        &piGui
    )) {{
        CloseHandle(piGui.hProcess);
        CloseHandle(piGui.hThread);
    }}

    ExitProcess(0);
}}
"""


def parse_coff_and_link_pe(obj_bytes: bytes, out_exe_path: str) -> None:
    machine, num_sections, _, sym_table_ptr, num_symbols, _, _ = struct.unpack_from(
        "<HHIIIHH", obj_bytes, 0
    )
    assert machine == 0x8664, f"Unexpected machine: {hex(machine)}"

    str_table_offset = sym_table_ptr + num_symbols * 18

    def get_sym_name(raw_name: bytes) -> str:
        zeroes, str_off = struct.unpack("<II", raw_name)
        if zeroes == 0:
            end = obj_bytes.find(b"\x00", str_table_offset + str_off)
            return obj_bytes[str_table_offset + str_off : end].decode("ascii")
        return raw_name.rstrip(b"\x00").decode("ascii")

    sections = []
    for i in range(num_sections):
        sh_off = 20 + i * 40
        (
            raw_name,
            vsize,
            vaddr,
            raw_size,
            raw_ptr,
            reloc_ptr,
            linenum_ptr,
            num_relocs,
            num_linenums,
            characteristics,
        ) = struct.unpack_from("<8sIIIIIIHHI", obj_bytes, sh_off)
        sec_name = get_sym_name(raw_name)
        data = bytearray(obj_bytes[raw_ptr : raw_ptr + raw_size]) if raw_ptr else bytearray()
        relocs = []
        for r in range(num_relocs):
            r_vaddr, r_sym_idx, r_type = struct.unpack_from(
                "<IIH", obj_bytes, reloc_ptr + r * 10
            )
            relocs.append((r_vaddr, r_sym_idx, r_type))
        sections.append(
            {
                "index": i + 1,
                "name": sec_name,
                "data": data,
                "relocs": relocs,
                "characteristics": characteristics,
            }
        )

    symbols = {}
    idx = 0
    while idx < num_symbols:
        s_off = sym_table_ptr + idx * 18
        raw_name = obj_bytes[s_off : s_off + 8]
        val, sec_num, sym_type, storage_class, num_aux = struct.unpack_from(
            "<IhHBB", obj_bytes, s_off + 8
        )
        name = get_sym_name(raw_name)
        symbols[idx] = {
            "name": name,
            "value": val,
            "sec_num": sec_num,
            "storage_class": storage_class,
        }
        idx += 1 + num_aux

    text_sec = next(s for s in sections if s["name"] == ".text")
    rdata_secs = [s for s in sections if s["name"].startswith(".rdata")]

    SECTION_ALIGN = 0x1000
    FILE_ALIGN = 0x200
    IMAGE_BASE = 0x140000000

    text_rva = 0x1000
    text_data = text_sec["data"]
    text_vsize = len(text_data)

    rdata_rva = text_rva + align_up(max(1, text_vsize), SECTION_ALIGN)
    merged_rdata = bytearray()
    rdata_sec_offsets = {}
    for rs in rdata_secs:
        while len(merged_rdata) % 16 != 0:
            merged_rdata.append(0)
        rdata_sec_offsets[rs["index"]] = len(merged_rdata)
        merged_rdata.extend(rs["data"])
    rdata_vsize = len(merged_rdata)

    idata_rva = rdata_rva + align_up(max(1, rdata_vsize), SECTION_ALIGN)

    num_imps = len(KERNEL32_IMPORTS)
    ilt_offset = 40
    iat_offset = ilt_offset + (num_imps + 1) * 8
    names_offset = iat_offset + (num_imps + 1) * 8

    idata = bytearray(names_offset)
    imp_symbol_rvas = {}
    hint_name_rvas = []

    for i, func_name in enumerate(KERNEL32_IMPORTS):
        if len(idata) % 2 != 0:
            idata.append(0)
        hn_rva = idata_rva + len(idata)
        hint_name_rvas.append(hn_rva)
        idata.extend(struct.pack("<H", 0))
        idata.extend(func_name.encode("ascii") + b"\x00")
        imp_symbol_rvas[f"__imp_{func_name}"] = idata_rva + iat_offset + i * 8

    dll_name_rva = idata_rva + len(idata)
    idata.extend(b"KERNEL32.dll\x00")

    for i, hn_rva in enumerate(hint_name_rvas):
        struct.pack_into("<Q", idata, ilt_offset + i * 8, hn_rva)
        struct.pack_into("<Q", idata, iat_offset + i * 8, hn_rva)

    struct.pack_into(
        "<IIIII",
        idata,
        0,
        idata_rva + ilt_offset,
        0,
        0,
        dll_name_rva,
        idata_rva + iat_offset,
    )
    idata_vsize = len(idata)

    def resolve_symbol_rva(sym_idx: int) -> int:
        sym = symbols[sym_idx]
        sname = sym["name"]
        if sname in imp_symbol_rvas:
            return imp_symbol_rvas[sname]
        sec_num = sym["sec_num"]
        if sec_num == text_sec["index"]:
            return text_rva + sym["value"]
        if sec_num in rdata_sec_offsets:
            return rdata_rva + rdata_sec_offsets[sec_num] + sym["value"]
        raise KeyError(f"Unresolved symbol: {sname} (sec_num={sec_num})")

    IMAGE_REL_AMD64_REL32 = 0x0004
    IMAGE_REL_AMD64_ADDR64 = 0x0001

    for r_vaddr, r_sym_idx, r_type in text_sec["relocs"]:
        target_rva = resolve_symbol_rva(r_sym_idx)
        instr_reloc_rva = text_rva + r_vaddr
        if r_type == IMAGE_REL_AMD64_REL32:
            existing_addend = struct.unpack_from("<i", text_data, r_vaddr)[0]
            rel32 = (target_rva + existing_addend) - (instr_reloc_rva + 4)
            struct.pack_into("<i", text_data, r_vaddr, rel32)
        elif r_type == IMAGE_REL_AMD64_ADDR64:
            existing_addend = struct.unpack_from("<q", text_data, r_vaddr)[0]
            struct.pack_into("<Q", text_data, r_vaddr, IMAGE_BASE + target_rva + existing_addend)
        else:
            raise ValueError(f"Unsupported relocation type: {r_type}")

    entry_rva = text_rva
    for sym in symbols.values():
        if sym["name"] == "WinMainCRTStartup":
            entry_rva = text_rva + sym["value"]
            break

    headers = bytearray(0x400)
    struct.pack_into("<2s", headers, 0, b"MZ")
    struct.pack_into("<I", headers, 0x3C, 0x80)

    pe_off = 0x80
    struct.pack_into("<4s", headers, pe_off, b"PE\x00\x00")

    num_pe_sections = 3
    size_of_optional_header = 240
    characteristics = 0x0001 | 0x0002 | 0x0020
    struct.pack_into(
        "<HHIIIHH",
        headers,
        pe_off + 4,
        0x8664,
        num_pe_sections,
        0,
        0,
        0,
        size_of_optional_header,
        characteristics,
    )

    opt_off = pe_off + 24
    size_of_image = idata_rva + align_up(idata_vsize, SECTION_ALIGN)
    size_of_headers = 0x400
    size_of_code = align_up(text_vsize, FILE_ALIGN)
    size_of_init_data = align_up(rdata_vsize, FILE_ALIGN) + align_up(idata_vsize, FILE_ALIGN)

    struct.pack_into(
        "<HBBIIIIIQIIHHHHHHIIIIHHQQQQII",
        headers,
        opt_off,
        0x020B,
        14,
        0,
        size_of_code,
        size_of_init_data,
        0,
        entry_rva,
        text_rva,
        IMAGE_BASE,
        SECTION_ALIGN,
        FILE_ALIGN,
        6,
        0,
        0,
        0,
        6,
        0,
        0,
        size_of_image,
        size_of_headers,
        0,
        2,
        0x0100,
        0x100000,
        0x10000,
        0x100000,
        0x1000,
        0,
        16,
    )

    dd_off = opt_off + 112
    struct.pack_into("<II", headers, dd_off + 1 * 8, idata_rva, idata_vsize)
    struct.pack_into(
        "<II",
        headers,
        dd_off + 12 * 8,
        idata_rva + iat_offset,
        (num_imps + 1) * 8,
    )

    sh_base = opt_off + 240

    text_raw_ptr = size_of_headers
    text_raw_size = align_up(text_vsize, FILE_ALIGN)

    rdata_raw_ptr = text_raw_ptr + text_raw_size
    rdata_raw_size = align_up(rdata_vsize, FILE_ALIGN)

    idata_raw_ptr = rdata_raw_ptr + rdata_raw_size
    idata_raw_size = align_up(idata_vsize, FILE_ALIGN)

    sec_defs = [
        (b".text\x00\x00\x00", text_vsize, text_rva, text_raw_size, text_raw_ptr, 0x60000020),
        (b".rdata\x00\x00", rdata_vsize, rdata_rva, rdata_raw_size, rdata_raw_ptr, 0x40000040),
        (b".idata\x00\x00", idata_vsize, idata_rva, idata_raw_size, idata_raw_ptr, 0xC0000040),
    ]

    for idx_s, (sname, vsz, rva, rsz, rptr, chars) in enumerate(sec_defs):
        struct.pack_into(
            "<8sIIIIIIHHI",
            headers,
            sh_base + idx_s * 40,
            sname,
            vsz,
            rva,
            rsz,
            rptr,
            0,
            0,
            0,
            0,
            chars,
        )

    exe_bytes = bytearray()
    exe_bytes.extend(headers)
    exe_bytes.extend(text_data.ljust(text_raw_size, b"\x00"))
    exe_bytes.extend(merged_rdata.ljust(rdata_raw_size, b"\x00"))
    exe_bytes.extend(idata.ljust(idata_raw_size, b"\x00"))

    os.makedirs(os.path.dirname(out_exe_path), exist_ok=True)
    with open(out_exe_path, "wb") as f:
        f.write(exe_bytes)


def main() -> None:
    with open(CS_PATH, "rb") as f:
        cs_bytes = f.read()

    c_src = generate_c_launcher(cs_bytes)
    c_path = "/tmp/free_overlay_launcher.c"
    obj_path = "/tmp/free_overlay_launcher.obj"

    with open(c_path, "w", encoding="utf-8") as f:
        f.write(c_src)

    subprocess.run(
        [
            "clang",
            "-target",
            "x86_64-pc-windows-msvc",
            "-Os",
            "-mno-sse",
            "-fno-builtin",
            "-fno-stack-protector",
            "-mno-stack-arg-probe",
            "-c",
            c_path,
            "-o",
            obj_path,
        ],
        check=True,
    )

    with open(obj_path, "rb") as f:
        obj_bytes = f.read()

    parse_coff_and_link_pe(obj_bytes, OUT_EXE)
    print(f"Built standalone Windows executable: {OUT_EXE} ({os.path.getsize(OUT_EXE)} bytes)")


if __name__ == "__main__":
    main()
