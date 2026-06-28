# 📁 Parallel Computing for File Compressor

This project explores and implements parallel computing technologies—such as CPU multi-threading, CPU cache optimization, and GPU acceleration—to significantly improve the performance of classic file compression algorithms (Deflate / LZMA).

The repository not only contains the source code for various algorithm optimizations for performance comparison, but also provides a complete, ready-to-use compression application with a Graphical User Interface (GUI), alongside a dedicated data integrity checking tool.

## 📂 Folder Structure & Descriptions

This project consists of four core directories. Here is a breakdown of what each folder contains:

### 1. `Compression App/` (Compiled Executables)
* **Purpose**: Contains the final compiled, ready-to-run executables.
* **Details**: If you simply want to test the final parallel compression application, you can directly run `run.exe` (the GUI application) or `MyParallelZipApp.exe` (the core backend console application) located here without setting up a development environment.

### 2. `Compression App Source Code/` (Application Source Code)
* **Purpose**: The complete source code for the end-user compression application.
* **Details**:
  * **`compressgui/`**: C# (Windows Forms) based Graphical User Interface source code for user interaction.
  * **`MyParallelZipApp/`**: The core backend parallel ZIP compression engine source code.
  * **`UZipDotNet-master/`**: The underlying ZIP compression library dependency source code.

### 3. `File Data Integrity Checker/` (Data Validator)
* **Purpose**: Ensures the compressed and decompressed file data is 100% accurate.
* **Details**: In parallel computing environments (especially with GPU and multi-threading), race conditions can potentially corrupt data. This is a standalone C# tool used to perform binary/hash comparisons between the original files and the decompressed files to guarantee data integrity.

### 4. `Parallel and Baseline Compressor/` (Algorithm Experiments)
* **Purpose**: The "experimental research" core of the project, containing the actual implementations of different hardware acceleration strategies.
* **Details**: This directory is divided by compression algorithm, and each algorithm features three distinct versions for performance benchmarking:
  * **`deflate（zip）/`** (Based on UZipDotNet):
    * `UZipDotNet-master basic version/`: The standard serial compression version (Baseline).
    * `UZipDotNet-master CPU+cache version/`: Accelerated version utilizing CPU multi-threading and cache optimization.
    * `UZipDotNet-master GPU version/`: Accelerated version utilizing GPU for massive parallel computing (via ILGPU).
  * **`Lzama（7z）/`** (Based on tiny7z for LZMA):
    * `tiny7z-master basic version/`: The standard serial compression version (Baseline).
    * `tiny7z-master CPU+cache version/`: Accelerated version based on CPU multi-core and cache optimization.
    * `tiny7z-master GPU version/`: Accelerated version based on GPU parallel computing.

## 🛠️ Technologies Used
* **Programming Language**: C#
* **Target Framework**: .NET 10.0
* **GUI Framework**: Windows Forms
* **GPU Parallel Framework**: [ILGPU](https://github.com/m4rs-mt/ILGPU) (for C# GPU acceleration)
* **Algorithms**: Deflate (Zip), LZMA (7z)

## 🚀 Getting Started

1. **Run Directly**: Navigate to the `Compression App/` folder and double-click `run.exe` to launch the software.
2. **Compile from Source**: 
   * Ensure you have Visual Studio or the .NET 10.0 SDK installed.
   * Open `Compression App Source Code/compressgui.slnx` to build or modify the GUI application.
   * To study or benchmark the algorithm performance differences, open the respective `.sln` or `.slnx` solution files inside the `Parallel and Baseline Compressor` directory.
3. **Data Verification**: After compressing and decompressing an archive, run the `File Data Integrity Checker` to verify that the extracted files match the original files perfectly.