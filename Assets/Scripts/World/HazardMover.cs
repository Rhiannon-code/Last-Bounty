using UnityEngine;

namespace FPSParkour.World
{
    [DisallowMultipleComponent]
    public class HazardMover : MonoBehaviour
    {
        [SerializeField] Vector3[] points;
        [SerializeField] float speed = 14f;
        [SerializeField] bool pingPong;
        [SerializeField] bool faceTravel = true;
        [SerializeField] float waitAtPoint;

        int index;
        int step = 1;
        float resumeAt;

        void Start()
        {
            if (points != null && points.Length > 0)
                transform.position = points[0];
        }

        void Update()
        {
            if (points == null || points.Length < 2 || Time.time < resumeAt)
                return;

            Vector3 destination = points[index];
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

            Vector3 heading = destination - transform.position;
            if (faceTravel && heading.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(heading);

            if ((transform.position - destination).sqrMagnitude > 0.01f)
                return;

            resumeAt = Time.time + waitAtPoint;
            Advance();
        }

        void Advance()
        {
            if (!pingPong)
            {
                index = (index + 1) % points.Length;
                return;
            }

            if (index + step < 0 || index + step >= points.Length)
                step = -step;

            index += step;
        }
    }
}
