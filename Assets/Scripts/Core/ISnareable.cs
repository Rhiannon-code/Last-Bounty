using UnityEngine;

namespace FPSParkour.Core
{
    public interface ISnareable
    {
        bool IsSnared { get; }
        void Snare(float seconds, Vector3 from);
    }
}
