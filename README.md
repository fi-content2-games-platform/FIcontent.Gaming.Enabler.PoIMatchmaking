PoIMatchmaking
==================

This package contains a sample server, a Unity client implementation, and a demo of the usage of the Unity package. A pre-built version of this servlet is included in the ["release" tab](https://github.com/fi-content2-games-platform/FIcontent.Gaming.Enabler.PoIMatchmaking/releases/latest). 
This document assumes that you are using this pre-built war file. For instructions on how to build the war file, see [this document](BuildAndDeploy.md).

Software Requirements
---------------------

* Apache Tomcat: [Download](http://tomcat.apache.org/) and install Tomcat (Installation guides can be found under the "Documentation" 
menu item on the same page). 

* Unity: required for the Unity demo. [Download](https://unity3d.com/get-unity) and follow the installation instructions. The Unity package and project were built with Unity 5.0.0f4. 
They have not been tests on later versions but are expected to work. They may fail to import cleanly on earlier versions.

Servlet
-------

The sample server is a Java Servlet intended to run in a Servlet Container such as Apache Tomcat. This servlet is included in the ["release" tab](https://github.com/fi-content2-games-platform/FIcontent.Gaming.Enabler.PoIMatchmaking/releases/latest).

####Apache Tomcat:

To run Tomcat, run ```<tomcat installation folder>/bin/startup```. If the server successfully started, the output should end with:
```
INFO: Server startup in <number> ms
````
The server should now be accessible from http://127.0.0.1:8080 (assuming the default values for the Tomcat setup have not been changed)

The GUI manager page at http://127.0.0.1:8080/manager can be used to deploy/stop war files. To deploy a war file, use the "WAR file 
to deploy" box, click "Choose file", navigate to the downloaded war file, select, then click "Deploy". All loaded war 
files are listed in the "Applications" box, and each has commands to stop, reload, etc., the application. The name of the war will
be what is needed to add to the url path to access the war.
To stop the Tomcat server, run ```<tomcat installation folder>/bin/shutdown```
See [this document](http://tomcat.apache.org/tomcat-4.1-doc/RUNNING.txt) for more details on running/stopping the Tomcat server.

Unity client package
--------------------

The Unity Client package is found at [/Unity/POIMatchmaking.unitypackage](/Unity)

After importing the client package into the Unity project, in order to match a client with another, apply the MatchClient component to
a GameObject, and initialize any public members that are needed to be customised.  The MatchClient component can be removed when 
matching is complete.

Note that in particular implementations of ILocationInterface and INetworkInterface will need to be provided - either
using the provided examples (UnityInputLocationInterface and UnityNetworkInterface), or custom implementations if different mechanisms are 
required for obtaining location data and initializing your networking library.

For more hints, see the example usage in the Unity Demo App for an example, in the 'Go' function of the 
POIMatchmakingDemo class.

Unity Demo App
--------------

The Unity Demo App is found at [Unity/POIMatchmakingDemo](Unity/POIMatchmakingDemo)

The Unity Demo App must be executed standalone, in order to run multiple instances, though one instance running 
in the editor can also connect to other standalone instances. It should work on all platforms, and has been tested on Windows Desktop and Android.

The client simulates connectivity issues according to three bits which can be toggled before pressing the 'Go' button. 
The initial setting of these bits is random, and the background colour also indicates their state.  Clients can only 
communicate with each other if they share at least one set bit in common.  This can be used loosely to simulate NAT 
traversal problems and other connectivity issues.

The client also provides an option to use a fake location interface, which allows a latitude/longitude position to be specified
instead of using Unity's location API, as that only works on mobile devices.  So, when testing on desktop, enable the 
fake location interface and optionally use the GUI fields to set the latitude/longitude values it should report.

The client is set to seek matches within a 0m radius, as this means that a client will only connect to other clients who are at the same POI; 
this is tweakable through the "Match radius (m)" field in the client GUI.

####Creating a Unity project to interact with the server:

1. Create a new project in Unity
2. In the top menu bar, click "Assets", "Import Package", "Custom Package". Ensure all files are ticked, and click "Import"
3. Open DemoScene.scene. There should be no errors in the output box
4. In the top menu bar, click "File", "Build Settings"
5. Under the "Scenes to Build" section, click "Add Current" 
6. Click the "Player Settings" button and open the "Resolution and Presentation" tab
  1. Ensure that "Default is Full Screen" is not ticked
  2. Set "Default Screen Width" to 600
  3. Set "Default Screen Height" to 900
  4. Ensure "Run In Background" is ticked
  5. Ensure "Resizeable Window" is ticked	
7. Click "Build" and choose the file name and where to save the file
8. Run a jetty or tomcat instance of the server
9. Open two instances of the client, ie., the program that has just been built
  - Ensure that all buttons in the GUI are visible, including "Quit" at the bottom.
  - If running on Windows, a "Windows Security Alert" may appear, as the application needs access to the  network to run. Allow the application access.
10. Type the url of the Tomcat server (including the path to the war file) in "Base Url" and click "Set"
11. Tick the box at "Fake Location", give them the same latitude/longitude position and click "Set"
12. Ensure that the clients have at least one conn bit in common, then click "Go"
13. The two clients should register with the service, and connect to each other. See the Tomcat server output to see the steps of the connection between the two clients

If while running the code, the following error appears: "Connection request to 67.225.180.24:50005 failed. Are you sure the server 
can be connected to", this may mean that the Unity Master Server is down. This Unity demo can still run locally on your machine by
running your own instance of the Unity Master Server. [Download "Master Server" and "Facilitator"](http://unity3d.com/master-server). 
Compile code and run MasterServer.exe and Facilitator.exe. Uncomment Awake() in UnityNetworkInterface.cs. The value for MasterServer.port 
will need to be changed to the value that is displayed when MasterServer.exe has been run.

The role of host is automatically assigned to a client by the server. Only a host will continue to loop through the code, checking for 
new incoming connections from additional clients. This means that a host can downgrade to a "normal" client, but not vice versa.

