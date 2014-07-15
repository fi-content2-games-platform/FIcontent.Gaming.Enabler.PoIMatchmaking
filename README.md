PoIMatchmaking
==================

This package contains a sample server, a Unity client implementation, and a demo of the usage of the Unity package. 
The server also includes a web-based Javascript client for test purposes.

Software Requirements
---------------------

To build the servlet you need a JDK and Maven.  The Java library dependencies will be downloaded automatically by 
Maven.  I use jdk1.7.0.25 from Oracle and Maven 3.1.1 from Apache.

The Unity package and project were built with Unity 4.2.2f1, and should work on later versions but may fail to 
import cleanly on earlier versions.

Servlet
-------

The sample server is a Java Servlet intended to run in a Servlet Container such as Apache Tomcat.

It is packaged as a Maven project, which may be built and deployed from the command line or imported into an 
IDE such as IntelliJ IDEA.

The Maven project uses the tomcat and jetty plugins.  The tomcat plugin primarily allows deployment to an Apache 
Tomcat server.  The jetty plugin provides a local servlet container, allowing you to more easily run a local 
instance of the servlet and test against that.

To use the tomcat maven plugin you may need to edit the relevant configuration section of pom.xml.  In particular 
it is currently set to deploy to host 'fi-cloud', which you can locally alias to an IP address via /etc/hosts 
(%WINDOWS%\system32\drivers\etc\hosts).  You may need to edit the other configuration settings, and also be aware 
that login/password details for managing the Tomcat server are stored for maven in a local configuration file 
(~/.m2/settings.xml), not in the git repository.  Google "tomcat-maven-plugin settings.xml" for more information.

The main maven goals that are useful here are:

<dl>
<dt>mvn tomcat:redeploy</dt>
<dd>Update the server with a new build of the servlet; this also restarts the perpetual running instance of the servlet</dd>
<dt>mvn jetty:run</dt>
<dd>Run the servlet locally using Jetty in the foreground</dd>
<dt>mvn jetty:start</dt>
<dd>Run the servlet locally using Jetty in the background</dd>
<dt>mvn jetty:stop</dt>
<dd>Kill any running Jetty instance, either in the foreground or the background</dd>
</dl>

Test web interface
------------------

When the servlet is running you can use the multi-client test web interface by pointing a web browser at 
http://localhost:8888/jstest/index.html if you're using Jetty, or http://fi-cloud:8080/matcher/jstest/index.html 
if you're using the standard fi-cloud deployment settings. Note that "matcher" in the URL refers the the war file generated 
by maven.

The clients pretty much manage themselves once started.  You get an opportunity to control the client-to-client 
connection process, which allows you to instead reject matches and check that the clients get rematched against
other peers.

The status line at the top of the display shows some statistics about the server - the number of active clients, 
the number of unmatched clients, the number of matches, and the number of clients that are pending deletion.

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

Before building the player, select the "Main Camera" game object and, in the Inspector, edit the "Base Url" setting of
the "SpatialMatchmakingDemo" component.  This specifies which instance of the servlet to use - for local Jetty use it 
should probably be http://localhost:8888, or with the default deployment settings you can use http://fi-cloud:8080/matcher, 
substituting for fi-cloud if you don't have a name alias set up in /etc/hosts.

The client simulates connectivity issues according to three bits which you can toggle before pressing the 'Go' button. 
The initial setting of these bits is random, and the background colour also indicates their state.  Clients can only 
communicate with each other if they share at least one set bit in common.  This can be used loosely to simulate NAT 
traversal problems and other connectivity issues.

The client also provides an option to use a fake location interface, which allows you to specify a lat/long position
instead of using Unity's location API, as that only works on mobile devices.  So, when testing on desktop, enable the 
fake location interface and optionally use the GUI fields to set the lat/long values it should report.

The client is set to seek matches within a 0m radius; this is tweakable through the MaxMatchRadius field in the 
Inspector.

If while running the code, the following error appears: "connection request to 67.225.180.24:50005 failed. Are you sure the server 
can be connected to", this may mean that the Unity Master Server is down. This Unity demo can still run locally on your machine 
running your own instance of the Unity Master Server. Download "Master Server" and "Facilitator" from http://unity3d.com/master-server
.Compile code and run MasterServer.exe and Facilitator.exe. Uncomment Awake() in UnityNetworkInterface.cs. The value for MasterServer.port 
will need to be changed to the value that is displayed when you run MasterServer.exe.

The role of host is automatically assigned to a client by the server. Only a host will continue to loop through the code, checking for 
new incoming connections from additional clients. This means that a host can downgrade to a "normal" client, but not vice versa.

A host can change to a role of a client, but a client can't change to a role of host. This is because once a client connects, it reaches 
the "break" in the loop, but a host continues to loop, and look for new matches
A host changes to a client when the following occurs: P1, P2 register. P2 is host. P2 and P1 try to connect to each other but fail (due to 
NAT error or requirements fail). P2 is still hosting and looking for an additional client. P3 registers. It first attempts to connect to P1, 
where P3 acts as the host. P3 and P1 fail to connect (due to NAT error or requirements fail), so P3 looks for additional clients. P3 starts 
to connect to P2, where P2 changes from host to client. P3 is still the host. Successful match: [3,2]
		
If a client is currently connecting to a host and the host quits the service, then the client automatically re-registers with the service and will begin to look for a new match
if a client has finished connecting to a host and the host quits the service, then the client will have to manually quit the service and re-register.
