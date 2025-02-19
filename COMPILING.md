## Compilation

Building the project is for users that want to contribute code only.
If you wish to build the emulator yourself, follow these steps:

### Step 1

Install the [.NET 9.0 (or higher) SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
Make sure your SDK version is higher or equal to the required version specified in [global.json](global.json).

### Step 2

Either use `git clone https://github.com/Ryubing/Ryujinx` on the command line to clone the repository or use Code --> Download zip button to get the files.

### Step 3

To build Ryujinx, open a command prompt inside the project directory.
You can quickly access it on Windows by holding shift in File Explorer, then right clicking and selecting `Open command window here`.
Then type the following command: `dotnet build -c Release -o build`
the built files will be found in the newly created build directory.

Ryujinx system files are stored in the `Ryujinx` folder.
This folder is located in the user folder, which can be accessed by clicking `Open Ryujinx Folder` under the File menu in the GUI.
