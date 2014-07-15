using System.Collections;

namespace Assets.SpatialMatchmaking
{
    public interface ILocationInterface
    {
        bool Ready { get; }
        Location Location { get; }

        IEnumerator Init(int timeLimit);
    }
}
