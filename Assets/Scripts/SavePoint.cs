using UnityEngine;

public class SavePoint : MonoBehaviour
{
    private static SavePoint _current;
    private static Vector3 _defaultRespawnPosition;
    private static bool _hasDefaultRespawn;

    public static void SetDefaultRespawnPosition(Vector3 position)
    {
        _defaultRespawnPosition = position;
        _hasDefaultRespawn = true;
    }

    public static Vector3 GetRespawnPosition()
    {
        if (_current != null)
        {
            return _current.transform.position;
        }

        return _defaultRespawnPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryActivate(collision.collider);
    }

    private void TryActivate(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (other.GetComponentInParent<Health>() == null)
        {
            return;
        }

        _current = this;
    }
}
