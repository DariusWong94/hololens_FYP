## HoloLens Mixed Reality Overlay For the Hearing Impaired
-------
### PC Requirements
-------
* 64-bit Windows 10 Version 1709 
    *	aka Fall Creators Update or Build 16299
    *	Go to: Settings > System > About to check your version of Windows
    *	If your PC is not up to date, go to: Settings > Update & security > Windows Update
* Graphics card with DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities
* At least 18-20 GB of available hard disk space to install software, store tutorial files, and to compile applications developed during tutorials


### Downloads
---------
* [Visual Studio](https://developer.microsoft.com/en-us/windows/downloads)
* [Unity3D](https://download.unity3d.com/download_unity/1f4e0f9b6a50/UnityDownloadAssistant-2017.2.2f1.exe)
* [HoloLens Emulator and Holographic Templates](http://download.microsoft.com/download/B/A/7/BA7320D5-020F-42C6-9D23-001E334FA34E/emulator/EmulatorSetup.exe)


### Setup Instructions
----
#### Visual studio:
1. **Open** download.exe
2. Select **Universal Windows Platform development**
3. Ensure that **C++ Universal Windows Platform Tools** is enabled when installing
4. Select **Install**

#### Unity 3D:
1. **Open** download.exe
2. Select **64-bit** archetecture
3. ensure that the **Unity 2017.2.2p2**, **Standard Assets**, **Windows Store .NET Scripting Backend** and **Windows Store IL2CPP Scripting Backend** components are enabled.
4. **Finish** the installation

### How To run
-----
1. Fork, clone project
2. Launch Unity3D
3. Click on **Open** and navigate to the **Hololens_Speech_Project** and press **Select Folder**
4. Wait for Unity to complete importing the project.
5. On launch go to **File > Build Settings**
6. Select **Universal Windows Platform**,
7. Taget device, **HoloLens**
8. Click on **Switch Platform**
9. build project and run on Visual Studio or Press the play button on Unity
