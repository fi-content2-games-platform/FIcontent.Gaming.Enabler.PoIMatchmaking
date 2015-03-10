using System.Collections.Generic;
using Assets.POIMatchmaking;
using UnityEngine;

namespace Assets
{
	public class POIMatchmakingDemo : MonoBehaviour
	{
		//public int MaxMatchRadius = 500;
		public GUISkin LargeGuiSkin;
		
		private MatchClient _matchClient;
		private int _connectivityBits;
		private string _key;
		private readonly List<string> _log = new List<string>();
		
		public string baseUrl = "http://localhost:8888"; //the default value for the base url
		private string _testBaseUrl = "http://localhost:8888"; 
		private TestLocationInterface _testLocationInterface;

		//just north-west of Nikolai church, Berlin (Germany)
		private string _testLatitude = "52.5168";
		private string _testLongitude = "13.407329";
		
		private bool _isGoClicked = false; //false if we have not registered yet. True if we have
		
		//for setting requirement of not Uuid
		private string _testReqNotUuid = "";
		private string _reqNotUuid = "";
		private string _reqNotUuidLabel = ""; //a string displaying all uuids, seperated by commas
		private List<string> dontMatchWith = new List<string>(); //the list containing all clients for this client's requirements.requireNotUuid

		//match radius
		private string _testMatchRadius = "0";
		public int matchRadius = 0; //m. The default radius for matching two clients

		//snap radius
		private string _testSnapRadius = "75";
		public int snapRadius = 75; //m. The default radius for when a client is searching for the nearest PoI
		
		
		private bool _useTestLocationInterface;
		private int _guiScale;
		
		public void Start()
		{
			_connectivityBits = 0;
			for (int i = 0; i < 3; ++i)
			{
				if (Random.value > 0.5f)
				{
					_connectivityBits |= 1 << i;
				}
			}
			
			UpdateBackgroundColor();
			
			_testLocationInterface = new TestLocationInterface();
			
			SetTestLocation(0.0, 0.0);
			
			_guiScale = Screen.width / 300;
			if (_guiScale < 1) _guiScale = 1;
			LargeGuiSkin.label.fontSize = 10 * _guiScale;
			LargeGuiSkin.button.fontSize = 13 * _guiScale;
			LargeGuiSkin.toggle.padding.left = 20 * _guiScale;
			LargeGuiSkin.toggle.padding.top = 20 * _guiScale;
		}
		
		private void UpdateBackgroundColor()
		{
			Camera.main.backgroundColor = new Color(
				(_connectivityBits & 1) > 0 ? 0.5f : 0.0f,
				(_connectivityBits & 2) > 0 ? 0.5f : 0.0f,
				(_connectivityBits & 4) > 0 ? 0.5f : 0.0f
				);
		}
		
		private void SetTestLocation(double latitude, double longitude)
		{
			_testLocationInterface.SetLocation(new Location { Latitude = latitude, Longitude = longitude });
		}
		
		public void OnGUI()
		{
			GUI.skin = LargeGuiSkin;
			int quarterWidth = Screen.width / 4;
			
			GUILayout.BeginArea(new Rect(20, 40, Screen.width - 40, Screen.height - 80));
			{
				GUILayout.BeginVertical();
				{
					//base url, the url of the server
					GUILayout.BeginHorizontal();
					GUILayout.Label("Base url: ", GUILayout.Width(quarterWidth));
					_testBaseUrl = GUILayout.TextField(_testBaseUrl, GUILayout.Width(quarterWidth*2));

					if(!_isGoClicked && GUILayout.Button("Set", GUILayout.ExpandWidth(false))) 
						//updating the connection url is currently not supported in the GUI
					{
						if (_testBaseUrl.EndsWith("/")) // remove end "/"
							_testBaseUrl = _testBaseUrl.Substring(0, _testBaseUrl.Length-1);
						baseUrl = _testBaseUrl;
					}

					GUILayout.EndHorizontal();					
					GUILayout.BeginHorizontal();
					GUILayout.Label("", GUILayout.Width(quarterWidth));
					GUILayout.Label(string.Format(baseUrl), GUILayout.Width(quarterWidth*2));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();

					//client location
					GUILayout.BeginHorizontal();
					GUILayout.Label("Fake location", GUILayout.Width(quarterWidth));
					_useTestLocationInterface = GUILayout.Toggle(_useTestLocationInterface, "");
					GUILayout.EndHorizontal();
					
					if (!_useTestLocationInterface)
					{
						if (Input.location.status == LocationServiceStatus.Stopped)
							Input.location.Start();
						if (Input.location.status == LocationServiceStatus.Running)
						{
							GUILayout.BeginHorizontal();
							GUILayout.Label("lat/long", GUILayout.Width(quarterWidth));
							GUILayout.Label(string.Format("{0} / {1}", Input.location.lastData.latitude,
							                              Input.location.lastData.longitude));
							GUILayout.EndHorizontal();
						}
					}
					
					if (_useTestLocationInterface)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label("lat/long", GUILayout.Width(quarterWidth));
						
						_testLatitude = GUILayout.TextField(_testLatitude, GUILayout.Width(quarterWidth));
						_testLongitude = GUILayout.TextField(_testLongitude, GUILayout.Width(quarterWidth));
						if (!_isGoClicked && GUILayout.Button("Set"))
						{
							double latitude;
							double longitude;
							if (double.TryParse(_testLatitude, out latitude) && double.TryParse(_testLongitude, out longitude))
								SetTestLocation(latitude, longitude);
						}
						if (_isGoClicked && GUILayout.Button("Update"))
						{
							Debug.Log ("update");
							double latitude;
							double longitude;
							if (double.TryParse(_testLatitude, out latitude) && double.TryParse(_testLongitude, out longitude))
							{
								SetTestLocation(latitude, longitude);
								StartCoroutine(_matchClient.UpdateClientInfo());
							}
						}
						GUILayout.FlexibleSpace();
						
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						GUILayout.Label("", GUILayout.Width(quarterWidth));
						GUILayout.Label(string.Format("{0}", _testLocationInterface.Location.Latitude), GUILayout.Width(quarterWidth));
						GUILayout.Label(string.Format("{0}", _testLocationInterface.Location.Longitude), GUILayout.Width(quarterWidth));
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
					}

					//match radius
					GUILayout.BeginHorizontal();
					GUILayout.Label("Match radius (m): ", GUILayout.Width(quarterWidth));
					_testSnapRadius = GUILayout.TextField(_testMatchRadius, GUILayout.Width(quarterWidth*2));
					
					if(!_isGoClicked && GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
						//updating the max match radius is currently not supported in the GUI
					{
						int.TryParse(_testMatchRadius, out matchRadius);
					}

					GUILayout.EndHorizontal();					
					GUILayout.BeginHorizontal();
					GUILayout.Label("", GUILayout.Width(quarterWidth));
					GUILayout.Label(string.Format("{0}", ""+matchRadius), GUILayout.Width(quarterWidth));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();

					//snap radius
					GUILayout.BeginHorizontal();
					GUILayout.Label("PoI snap radius (m): ", GUILayout.Width(quarterWidth));
					_testSnapRadius = GUILayout.TextField(_testSnapRadius, GUILayout.Width(quarterWidth*2));
					
					if(!_isGoClicked && GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
					{
						int.TryParse(_testSnapRadius, out snapRadius);
					}
					else if(_isGoClicked && GUILayout.Button("Update", GUILayout.ExpandWidth(false)))
					{
						int.TryParse(_testSnapRadius, out snapRadius);
						_matchClient.snapRadius = snapRadius;
						StartCoroutine(_matchClient.UpdateClientInfo());
					}
					GUILayout.EndHorizontal();					
					GUILayout.BeginHorizontal();
					GUILayout.Label("", GUILayout.Width(quarterWidth));
					GUILayout.Label(string.Format("{0}", ""+snapRadius), GUILayout.Width(quarterWidth));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					
					//requirements
					GUILayout.BeginHorizontal();
					GUILayout.Label("Don't match with Uuid =", GUILayout.Width(quarterWidth));	
					
					_testReqNotUuid = GUILayout.TextField(_testReqNotUuid, GUILayout.Width(quarterWidth*2));
					if (_isGoClicked && GUILayout.Button("Update", GUILayout.ExpandWidth(false)))
					{
						_reqNotUuidLabel += _testReqNotUuid + ",";
						_matchClient.UpdateRequireNotUuid(_testReqNotUuid);
						StartCoroutine (_matchClient.UpdateClientInfo());
					}
					if (!_isGoClicked && GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
					{
						_reqNotUuid = _testReqNotUuid;
						_reqNotUuidLabel += _reqNotUuid + ",";
						dontMatchWith.Add(_reqNotUuid);
					}
					if (_reqNotUuidLabel != "")
					{
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						GUILayout.Label(_reqNotUuidLabel, GUILayout.Width(quarterWidth*4f));	
					}
					GUILayout.EndHorizontal();
					
					if (_isGoClicked && GUILayout.Button("Get Nearest POI", GUILayout.ExpandWidth(false)))
					{
						StartCoroutine(_matchClient.GetNearestPOI());
					}
					
					//conn bits and blank space
					if (_matchClient == null)
					{
						{
							GUILayout.BeginHorizontal();
							GUILayout.Label("conn bits", GUILayout.Width(quarterWidth));
							var r = GUILayout.Toggle((_connectivityBits & 1) != 0, "");
							var g = GUILayout.Toggle((_connectivityBits & 2) != 0, "");
							var b = GUILayout.Toggle((_connectivityBits & 4) != 0, "");
							_connectivityBits = (r ? 1 : 0) | (g ? 2 : 0) | (b ? 4 : 0);
							UpdateBackgroundColor();
							GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();
						}
						
						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Go", GUILayout.ExpandWidth(false)))
							Go();
						GUILayout.EndHorizontal();
						
						GUILayout.FlexibleSpace();
					}
					else
					{
						var s = "";
						for (int i = 0; i < _log.Count; ++i)
							s += _log[i] + "\n";
						
						GUILayout.TextArea(s, GUILayout.ExpandHeight(true));
					}
					
					//Quit
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Quit", GUILayout.ExpandWidth(false)))
					{
						if (_matchClient != null)
						{
							StartCoroutine(_matchClient.DeleteClient());
						}
						
						Application.Quit();
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndArea();
			
			if (_key != null)
				GUI.Label(new Rect(0, Screen.height - 30, Screen.width, Screen.height - 20), _key);
		}
		
		private void Go()
		{
			_isGoClicked = true;
			
			var unityNetworkInterface = gameObject.AddComponent<UnityNetworkInterface>();
			unityNetworkInterface.DisplayDebugUI = true;
			unityNetworkInterface.DebugConnectivityBits = _connectivityBits;
			
			
			_matchClient = gameObject.AddComponent<MatchClient>();
			_matchClient.NetworkInterface = unityNetworkInterface;
			_matchClient.LocationInterface = _testLocationInterface;
			_matchClient.BaseUrl = baseUrl;
			_matchClient.snapRadius = snapRadius;
			_matchClient.GameName = "com.studiogobo.fi.SpatialMatchmaking.Unity.PoiMatchmakingDemo";
			_matchClient.MaxMatchRadius = matchRadius;
			_matchClient.OnSuccess += Success;
			//_matchClient.OnFailure += ...;
			_matchClient.OnLogEvent += ProcessLogEvent;
			
			if (!_useTestLocationInterface)
			{
				var locationInterface = new UnityInputLocationInterface();
				_matchClient.LocationInterface = locationInterface;
				_matchClient.OnSuccess += locationInterface.Dispose;
				_matchClient.OnFailure += locationInterface.Dispose;
			}

			if (dontMatchWith.Count != 0)
				_matchClient.dontMatchWith = dontMatchWith;;
		}
		
		
		private void Success()
		{
			if (Network.isServer)
			{
				_key = string.Format("{0}", (int)(10000*Random.value));
				GetComponent<NetworkView>().RPC("RpcSetKey", RPCMode.Others, _key);
			}
		}
		
		[RPC]
		public void RpcSetKey(string key, NetworkMessageInfo info)
		{
			_key = key;
		}
		
		private void ProcessLogEvent(bool isError, string message)
		{
			if (isError)
				_log.Add("    ERROR: " + message);
			else
				_log.Add(message);
		}
	}
}
