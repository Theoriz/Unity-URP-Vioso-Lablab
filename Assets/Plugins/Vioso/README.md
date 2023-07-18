# VIOSO Unity URP plugin

### (1/3) Download
Download the repository files and place them under Assets\Pluings\VIOSO
### (2/3) Activate
- **In the Project Window:** find the folder "Settings".
- Click on the URP variant that corresponds to your project settings, e.g "High Fidelity Renderer".
- From the Inspector click on "Add Renderer Feature" and select the "VIOSO Render Feature".
- In the shader input select "VIOSOWarpBlendPP".
- **In the Hierachy Window:** select your Unity cameras, and add the "VIOSOURPcamera.cs" script to each. This will enable the plugin to overwrite the frustum with the views calculated in the calibration file.
### (3/3) Configure
- Open the file VIOSOWarpBlend.ini: 
	- Define the path for the calibration file (.vwf), by default it is relative to the VIOSOWarpBlend.dll path.
	- Define the **[channel_name]**: camera name & **calibIndex=x** : index of the corresponding mappping in the calibration file.

### Read more:
+ VIOSO Plugin: https://helpdesk.vioso.com/documentation/integrate-3d-engines/unity/
+ VIOSOWarpBlend.ini Reference: https://helpdesk.vioso.com/documentation/api/viosowarpblend-ini-reference/
+ VIOSO API: https://bitbucket.org/vioso/vioso_api/src/master/

![Screenshot](https://bitbucket.org/vioso/unity_urp_plugin/raw/3e81149e6fe1e068126fd0c5ce5fc791fdcd57ff/Screenshot.JPG)