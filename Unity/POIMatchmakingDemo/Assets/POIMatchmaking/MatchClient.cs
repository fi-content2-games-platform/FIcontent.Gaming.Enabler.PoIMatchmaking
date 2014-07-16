//not working

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.POIMatchmaking
{
	/// <summary>
	/// Primary interface for communicating with the matchmaking service.
	/// 
	/// Apply this component to a GameObject and initialize the public fields to control the matchmaking process.
	/// </summary>
	public class MatchClient : MonoBehaviour
	{
		/// <summary>
		/// The INetworkInterface implementation to use for low level networking with other peers
		/// </summary>
		public INetworkInterface NetworkInterface;
		
		/// <summary>
		/// The ILocationInterface implementation to use when sending location data to the server
		/// </summary>
		public ILocationInterface LocationInterface;
		
		/// <summary>
		/// The base URL of the matchmaking service
		/// </summary>
		public string BaseUrl;
		//original value (in inspector): http://130.206.83.114:8080/matcher
		
		/// <summary>
		/// A game name, which is converted into a matchmaking requirement so you only match with other instances of the same game
		/// </summary>
		public string GameName;
		
		/// <summary>
		/// Maximum radius for location-based matching
		/// </summary>
		public int MaxMatchRadius;
		//original 500. Changing to 0 for POI matching, that way only clients at the same POI will match.
		
		/// <summary>
		/// Called after successful connection
		/// </summary>
		public event Action OnSuccess;
		
		/// <summary>
		/// Called on giving up, when MaxFailures matchmaking attempts have been made and failed
		/// </summary>
		public event Action OnFailure;
		
		/// <summary>
		/// Called during the connection process to report status changes and errors
		/// </summary>
		public event Action<bool, string> OnLogEvent;
		
		/// <summary>
		/// Indicates whether a connection has been established
		/// </summary>
		public bool Connected { get; private set; }
		
		/// <summary>
		/// Maximum number of failed matchmaking attempts before giving up
		/// </summary>
		public int MaxFailures = 10;
		
		/// <summary>
		/// Maximum number of seconds to wait for location service initialization
		/// </summary>
		public int LocationInitTimeout = 20;
		
		/// <summary>
		/// Delay after unexpected server errors, before reattempting matchmaking
		/// </summary>
		public int UnexpectedServerErrorRetryDelay = 5;
		
		/// <summary>
		/// Delay after failing to connect to a host, before retrying the connection
		/// </summary>
		public int ConnectToHostFailRetryDelay = 1;
		
		/// <summary>
		/// Duration a host will wait for a client to connect before giving up and requesting a new match
		/// </summary>
		public float HostWaitForClientTimeout = 20; //30
		
		/// <summary>
		/// Duration a client will wait while attempting to connect to a host before abandoning the connection
		/// </summary>
		public int ConnectToHostTimeout = 9;

		/// <summary>
		/// The amount of clients in a match. This value will be used by the host as an additional query parameter when checking for additional 
		/// ie if matchRecord: {"id":1, "clients": [2,1,3]} , then numClientsinPrevMatch = 3
		/// </summary>
		private int numClientsInMatch = 0; 

		/// <summary>
		/// the ID of the client using this script
		/// </summary>
		private int id = -1;

		/// <summary>
		/// Used to inialise unity server only once. True if the call to Network.InitializeServer was succesful. Else false.
		/// </summary>
		private bool IsServerInitiliazed = false;
	

		//variables used when registering a client
		WWW www;
		JsonObject postData = new JsonObject();
		JsonArray requirements = new JsonArray();
		Hashtable headers = new Hashtable(); 
		JsonObject clientData = new JsonObject();

		JsonObject poiSearchOptions = new JsonObject();
		public int snapRadius = 75; //m. The default radius for when a client is searching for the nearest POI
		private int maxSearchRadius = 2000; //m. The max search radius to use when searching for nearby POIs
		private int maxPoisReturned = 50; //the max amount of POIs to return when querying the database
		private String poiGeUrl = "http://130.206.80.175/api/poi"; //the path to POI-GE's radial_search.php
		//private String poiGeUrl = "http://localhost/poi-ge"; //the path to POI-GE's radial_search.php

		//used when the client manually updates it's record, set in SpatialMatchmakingDemo.cs, used as requireNotUuid
		public List<String> dontMatchWith = new List<String>(); //the list containing all clients for this client's requirements.requireNotUuid


		private void Log(string message)
		{
			if (OnLogEvent != null) OnLogEvent(false, message);
		}
		
		private void LogError(string message)
		{
			if (OnLogEvent != null) OnLogEvent(true, message);
		}
		
		private static JsonObject RequireAttribute(string attribute, params string[] values)
		{
			return new JsonObject {
				{ "@type", "requireAttribute" }, 
				{ "attribute", attribute }, 
				{ "values", new JsonArray(values) }
			};
		}
		
		private static JsonObject RequireNotUuid(string uuid)
		{
			return new JsonObject {
				{ "@type", "requireNotUuid" }, 
				{ "uuid", uuid }
			};
		}
		
		private static JsonObject RequireLocationWithin(int radius)
		{
			return new JsonObject {
				{ "@type", "requireLocationWithin" },
				{ "radius", radius }
			};
		}

		//get the nearest POI to this client, if there is a POI within a maxSearchRadius
		public IEnumerator GetNearestPOI()
		{
			var nearestPOIwww = new WWW(BaseUrl + string.Format("/clients/{0}/nearestPOI", id));
			yield return nearestPOIwww;

			if (www.error != null) {
				LogError("WWW error: " + www.error);			
			}
			JsonObject poi = new JsonObject(nearestPOIwww.text);

			if (poi.Contains ("pois")) 
			{ 
				poi = poi.GetObject ("pois");

				String uuid = "";
				using (var key = poi.GetEnumerator())
				{
					while (key.MoveNext()) { //there will only be one, as only one POI is stored for the client
						uuid = key.Current;
					}
				}
				poi = poi.GetObject (uuid);

				String name = poi.GetObject ("fw_core").GetObject ("name").GetString ("");
				Double lat = poi.GetObject ("fw_core").GetObject ("location").GetObject ("wgs84").GetNumber ("latitude");
				Double lon = poi.GetObject ("fw_core").GetObject ("location").GetObject ("wgs84").GetNumber ("longitude");

				var location = LocationInterface.Location;
				Log ("You are " + Convert.ToInt32(Distance (lat, lon, location.Latitude, location.Longitude)) + "m from " + name);
			}
			else
			{
				Log ("There are no POIs within a "+maxSearchRadius+"m radius of you");

			}
		}

		//update the client info on the server, called when using SpatialMatchmakingDemo to update various values
		public IEnumerator UpdateClientInfo()
		{
			//update location
			var location = LocationInterface.Location;
			Log ("updating location to " + location.Latitude + ", " + location.Longitude);
			
			postData.Set("location", new JsonObject {
				{"longitude", location.Longitude},
				{"latitude", location.Latitude}
			});

			//poi search options
			Log ("updating snapRadius to " + snapRadius);
			
			postData.Set("poiSearchOptions", new JsonObject {
				{"snapRadius", snapRadius},
				{"maxSearchRadius", maxSearchRadius},
				{"maxPoisReturned", maxPoisReturned},
				{"poiGeUrl", poiGeUrl}
			});

			//requirements
			postData.Set("requirements", requirements);

			www = new WWW(BaseUrl + string.Format("/clients/{0}/update", id), postData.ToByteArray(), headers);
			yield return www;
			
			if (www.error != null)
			{
				LogError("WWW error: " + www.error);
				Log("error while updating the location of this client");
				if (www.error.Contains("404")) 
					//client id not found. This can happen when a host leaves the session while this client is trying to connect to it
					//When a host is deleted, it deletes the match record, and all clients in the match record, including this client
				{
					Log ("Client id not found. Please re-register");
				}
			}
		}

		public void UpdateRequireNotUuid(String otherClientUuid)
		{
			requirements.Add(RequireNotUuid(otherClientUuid));
			dontMatchWith.Add (otherClientUuid);
		}

		public IEnumerator DeleteClient()//called when the "Quit" button is clicked
		{
			if (id != -1) 
				yield return new WWW (BaseUrl + string.Format ("/clients/{0}/delete", id) + "?removeFromMatch=true", new JsonObject ().ToByteArray ());
			
		}

		public IEnumerator Start()
		{
			bool register = true;


			// "failures" counts the number of times we hit error cases from the server, so we can retry on errors but still give up if it's really broken.
			// It doesn't necessarily increase each time through the loop.
			var failures = 0;


			//true if the player is the host of the session, else false
			bool isHost = false; //assigned a value in while-loop
			while (failures < MaxFailures)
			{

				// 1. 	the client registers when it begins to use the service
				// 2. 	when a client is in the process of connecting to the host, and the host leaves the session, the host will delete the match record and all client records.
				//		the client will automically re-register.
				if (register) 
				{
					Log("waiting for location");
					yield return StartCoroutine(LocationInterface.Init(LocationInitTimeout));
					if (!LocationInterface.Ready)
					{
						LogError("Location service failed");
						if (OnFailure != null)
							OnFailure();
						yield break;
					}

					requirements = new JsonArray
						{
							RequireAttribute("gameName", GameName),
							RequireLocationWithin(MaxMatchRadius)
						};

					// add (or re-add, if re-registering), all the clients that we don't want to connect with
					for (int i = 0; i < dontMatchWith.Count; i++) 
					{
						requirements.Add(RequireNotUuid(dontMatchWith[i]));
					}


					poiSearchOptions = new JsonObject {
						{"snapRadius", snapRadius},
						{"maxSearchRadius", maxSearchRadius},
						{"maxPoisReturned", maxPoisReturned},
						{"poiGeUrl", poiGeUrl}
					};

					Log("registering");
					var location = LocationInterface.Location;
					
					postData = new JsonObject {
						{ "uuid", Guid.NewGuid().ToString() },
						{ "connectionInfo", NetworkInterface.GetConnectionInfo() },
						{ "location", new JsonObject {
								{"longitude", location.Longitude},
								{"latitude", location.Latitude},
							}},
						{ "requirements", requirements },
						{ "poiSearchOptions", poiSearchOptions}
					};
					
					headers = new Hashtable();
					headers["Content-Type"] = "application/json";
					www = new WWW(BaseUrl + "/clients", postData.ToByteArray(), headers);
					yield return www;


					if (www.error != null)
					{
						Debug.LogError("WWW error: " + www.error);
						LogError("registration failed");
						yield break;
					}
					
					if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
					{
						Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
						LogError("registration failed");
						yield break;
					}
					
					clientData = new JsonObject(www.text);
					id = clientData.GetInteger ("id");
					Log ("Client: " + id);
					register = false;
				}


				//we are now ready to make a match
				Log("waiting for match");
				while (true)
				{
					www = new WWW(BaseUrl + string.Format("/matches?client={0}", id) + "&numMatched=" + numClientsInMatch);

					yield return www;
					
					if (www.error != null)
					{
						LogError("WWW error: " + www.error);
						yield break;
					}
					
					if (www.text == "")
					{
						Log("still waiting for match");
						continue;
					}
					break;
				}
				if (www.error != null)
				{
					Log("wait-for-match failure, trying again in a while");
					++failures;
					yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
					continue;
				}
				
				Log("fetching match data");
				var sessionId = new JsonObject(www.text).GetInteger("id");
				
				www = new WWW(BaseUrl + string.Format("/matches/{0}", sessionId));
				yield return www;
				
				if (www.error != null)
				{
					LogError("WWW error: " + www.error);
					Log("failed to fetch match data, trying again in a while");
					++failures;
					yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
					continue;
				}
				
				var clients = new JsonObject(www.text).GetArray("clients");
				numClientsInMatch = clients.Count; //update for next www

				var otherClient = 0; //always starts from 1				
				isHost = clients.GetInteger(0) == id;
				if (isHost)
					otherClient = clients.GetInteger(numClientsInMatch-1); //last in array is the newest client to be added to the match
				else
					otherClient = clients.GetInteger(0); //the host of the match

				Log("fetching data for client "+otherClient);
				
				www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
				yield return www;
				
				if (www.error != null)
				{
					LogError("WWW error: " + www.error);
					Log("failed to fetch other client data, trying again in a while");
					++failures;
					yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
					continue;
				}
				
				var otherClientData = new JsonObject(www.text);

				isHost = clients.GetInteger(0) == id;
				if (isHost)
				{
					Log("HOSTING - waiting for client "+otherClient+" to join");

					var startTime = Time.realtimeSinceStartup;

					if(!IsServerInitiliazed)
						IsServerInitiliazed = NetworkInterface.InitServer();
					else //the server has already been initialised
						NetworkInterface.AddAnotherClient(); //reset the values for UnityNetworkInterface.Connecting and .Connected to false


					NetworkInterface.StartListening(otherClientData.GetString("uuid"));
					if (IsServerInitiliazed)
					{
						while (!NetworkInterface.Connected)
						{
							if (Time.realtimeSinceStartup - startTime > HostWaitForClientTimeout)
							{
								Log("Timeout waiting for client to connect");
								yield return new WaitForSeconds(1);
								break;
							}
							yield return null;
						}
					}
					else
					{
						LogError(NetworkInterface.NetworkError);
						Log("failed to initialize as host");
						yield return new WaitForSeconds(1);
					}
				}
				else
				{
					// This really shouldn't be here.  We probably need a way for the host to not fill in the connectionInfo until 
					// it is ready to accept connections, and this delay should be replaced by the client polling for that info to 
					// become available.  (The barrier to this is that currently updating client info clears all matches...)
					Log("waiting to let the host start");
					yield return new WaitForSeconds(3); 

					//test
					Log ("host connInfo: " + otherClientData.GetString("connectionInfo"));
					while (otherClientData.GetString("connectionInfo") == "")
					{
						yield return new WaitForSeconds(2);
						www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
						yield return www;
						otherClientData = new JsonObject(www.text);
					}
					Log ("host connInfo: " + otherClientData.GetString("connectionInfo"));

					var attempts = 0;
					while (!NetworkInterface.Connected)
					{
						Log("connecting to host");
						var startTime = Time.realtimeSinceStartup;

						var startConnectingOk = NetworkInterface.StartConnecting(otherClientData.GetString("connectionInfo"), clientData.GetString("uuid"));
						var timedOut = false;
						
						if (startConnectingOk)
						{
							while (NetworkInterface.Connecting && !timedOut)
							{
								timedOut = Time.realtimeSinceStartup - startTime > ConnectToHostTimeout;
								yield return null;
							}
						}
						
						if (NetworkInterface.Connected)
							continue;
						
						if (!startConnectingOk)
						{
							LogError(NetworkInterface.NetworkError);
							Log("error connecting to host - trying again");
						}
						else if (timedOut)
						{
							Log("timeout connecting to host - trying again");
						}
						else
						{
							LogError(NetworkInterface.NetworkError);
							Log("error connecting to host - trying again");
						}
						
						++attempts;
						if (attempts >= 3) break;
						yield return new WaitForSeconds(ConnectToHostFailRetryDelay);
					}
				}
				
				if (!NetworkInterface.Connected)
				{
					numClientsInMatch -= 1; //-1, since the client that we were trying to connect to has been removed from the match record on the server
					NetworkInterface.StopListening();
					Log("giving up connecting, will find another match. Cannot connect to " + otherClient);

					// We failed to connect to the peer, so explicitly ask the server not to match us with the same peer again
					requirements.Add(RequireNotUuid(otherClientData.GetString("uuid")));
					postData.Set("requirements", requirements);
					www = new WWW(BaseUrl + string.Format("/clients/{0}/update", id), postData.ToByteArray(), headers);
					yield return www;
					
					if (www.error != null)
					{
						LogError("WWW error: " + www.error);
						Log("error while updating requirements to exclude this partner");
						if (www.error.Contains("404")) 
							//client id not found. This can happen when a host leaves the session while this client is trying to connect to it
							//When a host is deleted, it deletes the match record, and all clients in the match record, including this client
						{
							Log ("Client id not found. Re-register");
							register = true;
						}
					}
					
					++failures;
					yield return new WaitForSeconds(1);
					continue;
				}
				
				// connected
				Connected = true;
				Log("Connected");

				if (!isHost)
					break;
				else
					NetworkInterface.StopListening();
			}
			
			//	only reached if !isHost			
			if (Connected && OnSuccess != null) 
			{
				OnSuccess ();								
			} 
			else if (!Connected && OnFailure != null) 
				OnFailure ();
			
		}

		//the haversince formula, from http://damien.dennehy.me/blog/2011/01/15/haversine-algorithm-in-csharp/
		private double Distance(double lat1, double lon1, double lat2, double lon2)
		{
			double dLat = ToRad(lat2 - lat1);
			double dLon = ToRad(lon2 - lon1);

			// a is the square of half the straight-line distance between the points, on a unit sphere
			double a = Math.Pow(Math.Sin(dLat / 2), 2) +
				Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
					Math.Pow(Math.Sin(dLon / 2), 2);

			// c is the arc angle between the points
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			// d is the arc length on a sphere of radius R
			double R = 6371000; // m
			double d = R * c;

			return d;
		}

		private double ToRad(double input)
		{
			return input * (Math.PI / 180);
		}

	}
}