using System.Collections;

namespace Assets.POIMatchmaking
{
    public interface ILocationInterface
    {
        bool Ready { get; }
        Location Location { get; }

        IEnumerator Init(int timeLimit);
    }
}
