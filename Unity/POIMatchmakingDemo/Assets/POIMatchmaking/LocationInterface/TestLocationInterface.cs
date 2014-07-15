using System.Collections;

namespace Assets.SpatialMatchmaking
{
	class TestLocationInterface : ILocationInterface
	{
	    public bool Ready
	    {
	        get { return true; }
        }

        public Location Location { get; private set; }

        public IEnumerator Init(int timeLimit)
        {
            yield break;
        }

        public void SetLocation(Location location)
        {
            Location = location;
        }
    }
}
