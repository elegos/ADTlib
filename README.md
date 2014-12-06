# ADTlib #

by Giacomo Furlan <giacomo@giacomofurlan.name>

**ADTlib** is a C# / .NET 4.5 library which aims to help the developer to use the Android development tools (adb, fastboot etc) inside a .net project / solution.

It wraps the original executables and executes them, interpreting the output and returning feedback, or the output itself if desired.

**This software is currently in development.**

---

## Work schedule ##
- ✔ **Resources** copy (executables and relative libraries) & basic execution from `%AppData%`
- **Basic ADB commands**
	- ✔ Get the devices' list (see `Device`)
	- ✔ Execute a generic command with parameters (with or without returning output)
	- ✔ Push a file to the device
	- ✔ Pull a file from the device
	- ✔ Delete a file from the device
	- ✔ Execute a shell command
	- ✔ Install an application from an APK hosted on the computer
	- ✔ Uninstall an application
	- ✔ Do a backup
	- ✔ Do a restore from a previous backup
	- Reboot
- **Advanced ADB commands**
	- Start and kill the adb server
	- (Re)start the adb server as root
	- Remount the /system partition in read/write
	- Switch between ADB TCP and USB mode
- **Basic Fastboot commands** (TBA)
- **Advanced Fastboot commands** (TBA)
- **Device**
	- ✔ Read the build.prop file and get any of its properties
	- ✔ Read the device's state on the fly
	- ✔ Read the device's serial number

---
## Usage ##
Simply import the ADTlib project (not solution) in your solution, or build it and import / reference the dll directly. The `Tests` project is not built in the release mode in any case.

It is designed so that you should only need to get the instance of `Adb` via `Adb.Instance` and use its methods. Once completed, there will be the `Fastboot` class, with the same usage. Each method has its own summary.

You may though want to know what the various classes do:

- `Adb`: an interface for adb.exe. It's a singleton.
- `Build`: the build.prop reader.
- `Device`: the device's model. Contains the serial number, the build.prop instance (Build class) and the state of the device.
- `Utils\Exe`: a utility class to easily execute adb/fastboot commands.
- `Utils\ResourcesManager`: a utility class to automatically extract adb, fastboot and their DLLs to an %AppData% subfolder; to retrieve their location (execution path). It's a singleton.
- `Extensions.cs` file: some useful string extensions for the ADB arguments.

---
## Changelog ##
0.5.0.0

- Added Adb.Backup
- Added Adb.Restore
- Added a (still unused) string extension to wait for a file lock

0.4.5.0

- Modified non-parametrical method names (from \*NoParams to \*NoParametric)
- Added Adb.Uninstall

0.4.0.0

- Created non-parametrical method aliases for low-level commands
- Added Adb.Install

0.3.0.0

- Using parametrical arguments
- Added Adb.Shell

0.2.0.0

- The state of the device is being read dynamically executing `adb get-state`


0.1.0.0

- Initial commit