using UnityEngine;
using UnityEngine.AI;

public static class SpawnUtility
{
    public static bool TryGetRandomNavMeshPosition(
        Vector3 center,
        float radius,
        out Vector3 position,
        int maxAttempts = 30
    )
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate =
                center
                + new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }
}
