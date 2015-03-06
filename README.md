PoIMatchmaking
==================

This package contains a sample server, a Unity client implementation, and a demo of the usage of the Unity package. 
The server also includes a web-based Javascript client for test purposes.

Software Requirements
---------------------

To build the servlet you need a JDK and Maven.  The Java library dependencies will be downloaded automatically by 
Maven.  I used jdk1.7.0.25 from Oracle and Maven 3.1.1 from Apache.
You will need to [download Maven](http://maven.apache.org/download.cgi) and follow the [installation instructions] 
(http://maven.apache.org/download.cgi#Installation)

The Unity package and project were built with Unity 5.0.0f4, and should work on later versions but may fail to 
import cleanly on earlier versions.

Servlet
-------

The sample server is a Java Servlet intended to run in a Servlet Container such as Apache Tomcat.

It is packaged as a Maven project, which may be built and deployed from the command line or imported into an 
IDE such as IntelliJ IDEA.

The Maven project uses the tomcat and jetty plugins.  The tomcat plugin primarily allows deployment to an Apache 
Tomcat server.  The jetty plugin provides a local servlet container, allowing you to more easily run a local 
instance of the servlet and test against that.

The section of pom.xml containing settings for Tomcat/Jetty:
```xml
	<build>
        <plugins>
            <plugin>
                <groupId>org.mortbay.jetty</groupId>
                <artifactId>jetty-maven-plugin</artifactId>
                <version>7.2.0.v20101020</version>
                <configuration>
                    <systemProperties>
                        <systemProperty>
                            <name>jetty.port</name>
                            <value>8888</value>
                        </systemProperty>
                    </systemProperties>

                    <stopPort>8889</stopPort>
                    <stopKey>ApparentlyNotOptional</stopKey>
                </configuration>
            </plugin>
            <plugin>
                <groupId>org.codehaus.mojo</groupId>
                <artifactId>tomcat-maven-plugin</artifactId>
                <version>1.1</version>
                <configuration>
                    <url>http://fi-cloud:8080/manager</url>
                    <server>TomcatServer</server>
                    <path>/matcher</path>
                </configuration>
            </plugin>
        </plugins>
    </build>
```
	
To build the war file:
use a terminal/command prompt to change the directory to that containing the pom.xml file and run: ```mvn package```
You should see text scrolling, and then
```
[INFO] ------------------------------------------------------------------------
[INFO] BUILD SUCCESS
[INFO] ------------------------------------------------------------------------
[INFO] Total time: <some value>
[INFO] Finished at: <some value>
[INFO] Final Memory: <some value>
[INFO] ------------------------------------------------------------------------
```

####Tomcat:
[Download and install Tomcat](http://tomcat.apache.org/) (Installation guides can be found under the "Documentation" 
menu item on the same page). To use the tomcat maven plugin you may need to edit the relevant configuration section Tomcat's 
entry in the plugin section of pom.xml. In particular it is currently set to deploy to host 'fi-cloud', which you can locally 
alias to an IP address via /etc/hosts (%WINDOWS%\system32\drivers\etc\hosts). 
Eg. you can add the following entry to your hosts file
```
127.0.0.1		fi-cloud
```

You may need to edit the other configuration settings, and also be aware that login/password details for managing the Tomcat 
server are stored for maven in a local configuration file (<maven installation folder>/conf/settings.xml), not in the git 
repository. Google "tomcat-maven-plugin settings.xml" for more information.
	
To run Tomcat on Windows, run ```<tomcat installation folder>/bin/startup```. If the server successfully started, you should see:
```
INFO: Server startup in <number> ms
````
Assuming that Tomcat is configured to run on port 8080, it should now be accessible from http://fi-cloud:8080
You can use GUI manager page at http://fi-cloud:8080/manager to deploy/stop war files. To deploy a war file, use the "WAR file 
to deploy" box, click "Choose file", navigate to the war file you wish to deploy, select, then click "Deploy". All loaded war 
files are listed in the "Applications" box, and each has commnds to stop, reload, etc., the application. The name of the war will
be what you need to add to the url path to access the war.
To stop the Tomcat server, run ```<tomcat installation folder>/bin/shutdown```
See [this document](http://tomcat.apache.org/tomcat-4.1-doc/RUNNING.txt) for more details on running/stopping your tomcat server

####Jetty:
To run jetty, use a terminal/command prompt to navigate to the folder that contains the pom.xml file and run: 
```mvn jetty:run```
If successful, you should see
```
[INFO] Started Jetty Server
```
Jetty will now be running on the port specified in pom.xml, eg. http://127.0.0.1:8888 or http://fi-cloud:8888 if you have added it
to your hosts file
To stop: type Ctrl+c, type 'y', press Enter button

Unity client package
--------------------

After importing the client package into your project, when you want to matchmake apply the MatchClient component to
a GameObject, and initialize any public members you need to customize.  You can remove the MatchClient component when 
matching is complete.

Note that in particular you will need to provide implementations of ILocationInterface and INetworkInterface - either
using the provided examples (UnityInputLocationInterface and UnityNetworkInterface), or custom implementations if you 
require different mechanisms for obtaining location data and initializing your networking library.

For more hints, see the example usage in the Unity Demo App for an example, in the 'Go' function of the 
POIMatchmakingDemo class.

Unity Demo App
--------------

The Unity Demo App must be executed standalone, in order to run multiple instances, though one instance running 
in the editor can also connect to other standalone instances.

It should work on all platfroms, and has been tested on Windows Desktop and Android.  There are some caveats on mobile,
however, due to Unity's poor WWW interface.

The client simulates connectivity issues according to three bits which you can toggle before pressing the 'Go' button. 
The initial setting of these bits is random, and the background colour also indicates their state.  Clients can only 
communicate with each other if they share at least one set bit in common.  This can be used loosely to simulate NAT 
traversal problems and other connectivity issues.

The client also provides an option to use a fake location interface, which allows you to specify a lat/long position
instead of using Unity's location API, as that only works on mobile devices.  So, when testing on desktop, enable the 
fake location interface and optionally use the GUI fields to set the lat/long values it should report.

The client is set to seek matches within a 0m radius, as this means that a client will only connect to other clients who are at the same POI; 
this is tweakable through the MaxMatchRadius field in the Inspector.

The following instructions can be used to create a unity project and interact with the server:

1. Create a new project in Unity
2. In the top menu bar, click Assets, Import Package, Custom Package. Ensure all files are ticked, and click "Import"
3. Open DemoScene.scene. There should be no errors in the output box
4. In the top menu bar, click File, Build Settings
5. Under the "Scenes to Build" section, click "Add Current". 
6. Click the "Player Settings" button and open the "Resolution and Presentation" tab
  1. Ensure that "Default is Full Screen" is not ticked
  2. Set Default Screen Width to 400
  3. Set Default Screen Height to 800
  4. Ensure Run In Background is ticked
  5. Ensure "Resizeable Window" is ticked	
7. Click Build and choose wthe file name and where to save the file.
8. Run a jetty or tomcat instance of the server
9. Open two instances of the client, ie., the program you have just built
  - Ensure that you can see the all buttons in the GUI, including "Quit" at the bottom.
  - If running on Windows, you may get a "Windows Security Alert", as the application needs access to your network to run. It is recommended to select Domain and/or Private network
10. Type the url of your server in "Base Url" and click "Set"
11. To test that two clients can connect, give them the same lat/long values, make sure they have at least one conn bit in common, then click "Go"
  - Unless you have provided provide implementations of ILocationInterface and INetworkInterface, you will need to tick the box at "Fake Location", and enter a latitude/longitude value, or use the default lat/long value, then click "Set".
12. The two clients should register with the service, and connect to each other. See the jetty/tomcat server output to see the steps of the connection between the two clients



If while running the code, the following error appears: "Connection request to 67.225.180.24:50005 failed. Are you sure the server 
can be connected to", this may mean that the Unity Master Server is down. This Unity demo can still run locally on your machine by
running your own instance of the Unity Master Server. [Download "Master Server" and "Facilitator"](http://unity3d.com/master-server). 
Compile code and run MasterServer.exe and Facilitator.exe. Uncomment Awake() in UnityNetworkInterface.cs. The value for MasterServer.port 
will need to be changed to the value that is displayed when you run MasterServer.exe.

The role of host is automatically assigned to a client by the server. Only a host will continue to loop through the code, checking for 
new incoming connections from additional clients. This means that a host can downgrade to a "normal" client, but not vice versa.

