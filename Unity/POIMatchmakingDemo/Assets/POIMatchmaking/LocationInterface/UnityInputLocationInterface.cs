using System;
using System.Collections;
using UnityEngine;

namespace Assets.SpatialMatchmaking
{
    internal class UnityInputLocationInterface : ILocationInterface, IDisposable
    {
        public bool Ready { get; private set; }

	    public Location Location
	    {
	        get
	        {
                if (Input.location.status != LocationServiceStatus.Running)
                    throw new ApplicationException("Location service not running");

	            var location = Input.location.lastData;
	            return new Location {Latitude = location.latitude, Longitude = location.longitude};
	        }
	    }

	    public IEnumerator Init(int timeLimit)
	    {
            Input.location.Start();

	        var wait = timeLimit;
            while (wait-- > 0 && Input.location.status == LocationServiceStatus.Initializing)
                yield return new WaitForSeconds(1);

	        Ready = (Input.location.status == LocationServiceStatus.Running);
	    }

        public void Dispose()
        {
            Input.location.Stop();
        }
    }
}
