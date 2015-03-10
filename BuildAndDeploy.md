This document contains instructions on how to build and deploy an instance of the POI Matchmaking SE.

Software Requirements
---------------------

* JDK: The latest JDK can be found on the [Oracle website](http://www.oracle.com/technetwork/java/javase/downloads/index.html)
* Apache Maven: [Download](http://maven.apache.org/download.cgi) and follow the [installation instructions] (http://maven.apache.org/download.cgi#Installation). The Java library dependencies will be downloaded automatically by 
Maven. 
* Apache Tomcat: needed only if you wish to deploy the servlet using Tomcat. [Download](http://tomcat.apache.org/) and install (Installation guides can be found under the "Documentation" 
menu item on the same page). 

Servlet
-------

The Maven project uses the Tomcat and Jetty plugins.  The Tomcat plugin primarily allows deployment to an Apache 
Tomcat server.  The Jetty plugin provides a local servlet container, allowing you to more easily run a local 
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
                    <server>TomcatServer</server>
                    <path>/matcher</path>
                </configuration>
            </plugin>
        </plugins>
    </build>
```

####Tomcat:
Tomcat can be used either as a standalone (ie, using the GUI to deploy a war to the server), or using Maven.

#####Standalone:
* Build a war file (see "Building a war file" below)
* Run ```<tomcat installation folder>/bin/startup```. If the server successfully started, you should see:
```
INFO: Server startup in <number> ms
````
The server should now be accessible from http://127.0.0.1:8080 (assuming the default values for the Tomcat setup have not been changed)

* The GUI manager page at http://127.0.0.1:8080/manager can be used to deploy/stop war files. To deploy a war file, use the "WAR file 
to deploy" box, click "Choose file", navigate to the desired war file, select, then click "Deploy". All loaded war 
files are listed in the "Applications" box, and each has commands to stop, reload, etc., the application. The name of the war will
be what is needed to add to the url path to access the war.
To stop the Tomcat server, run ```<tomcat installation folder>/bin/shutdown```
See [this document](http://tomcat.apache.org/tomcat-4.1-doc/RUNNING.txt) for more details on running/stopping the Tomcat server.

#####Running Tomcat using Maven:
The relevant configuration section Tomcat's entry in the plugin section of pom.xml can be edited. You may need to edit the other configuration settings, 
and also be aware that login/password details for managing the Tomcat server are stored for maven in a local configuration file (<maven installation folder>/conf/settings.xml), 
not in the git repository. Google "tomcat-maven-plugin settings.xml" for more information.

* To run Tomcat, use a terminal/command prompt to navigate to the folder that contains the pom.xml file and run:
```mvn tomcat:run```
If successful, the output should end:
```
INFO: Starting Coyote HTTP/1.1 on http-8080
```
Tomcat will now be running on the path specified in pom.xml, eg. http://localhost:8080/matcher
* To stop: type Ctrl+c, type 'y', press Enter button

####Jetty:
* To run Jetty, use a terminal/command prompt to navigate to the folder that contains the pom.xml file and run: 
```mvn jetty:run```
If successful, the output should end:
```
[INFO] Started Jetty Server
```
Jetty will now be running on the port specified in pom.xml, eg. http://localhost:8888
* To stop: type Ctrl+c, type 'y', press Enter button


#####Building a war file: 
Use a terminal/command prompt to change the directory to that containing the pom.xml file and run: ```mvn package```
The output would end:
```
[INFO] ------------------------------------------------------------------------
[INFO] BUILD SUCCESS
[INFO] ------------------------------------------------------------------------
[INFO] Total time: <some value>
[INFO] Finished at: <some value>
[INFO] Final Memory: <some value>
[INFO] ------------------------------------------------------------------------
```
Additional compilation help is beyond the scope of this document.