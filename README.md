# cuda-mosaic
## Mosaic Image Builder 
This project shows how to create an image mosaic using a source folder of small images of the same size while leveraging the power of a CUDA enabled GPU to do all of the hard comparison work. The console app is programmed using C# and the .NET Framework, but the GPU kernel is done with C++.

### Pre-Requisites:
- [Visual Studio 2017 Community Edition](https://www.visualstudio.com/downloads/)
  - Install both the `C#` and the `C++` development platforms
- [CUDA 9.0 SDK](https://developer.nvidia.com/cuda-90-download-archive)
- Registry Key
  - `Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers`
  - Add `DWORD` named `TdrLevel`
  - Set value to `0`
  - This will allow the GPU to perform calulations for longer than 2 seconds, but will lock up the screen if using the primary card as the CUDA processor
