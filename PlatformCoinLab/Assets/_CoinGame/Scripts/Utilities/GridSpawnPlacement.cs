using System.Collections.Generic;
using UnityEngine;

namespace CoinGame.Utilities
{
    public static class GridSpawnPlacement
    {
        public static bool TrySelect(GridSpawnMap map, GridSpawnKind kind, GameObject prefab,
            SpawnPlacementSettings settings, out Pose pose, out int reservationId, out bool usedFallback)
        {
            pose = default;
            reservationId = 0;
            usedFallback = false;
            GridSpawnReservation.Configure(map);
            List<int> candidates = new();
            for (int i = 0; i < map.Cells.Length; i++)
            {
                if (GridSpawnReservation.CanReserveIndex(i))
                {
                    candidates.Add(i);
                }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                (candidates[i], candidates[other]) = (candidates[other], candidates[i]);
            }

            float bestClearance = -1f;
            int selected = -1;
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            foreach (int index in candidates)
            {
                Vector3 position = map.Cells[index].WorldPosition;
                int attempts = 1;
                if (settings != null)
                {
                    position += settings.PositionOffset;
                    attempts = Mathf.Max(1, settings.RotationAttempts);
                }

                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    Quaternion rotation = prefab.transform.rotation;
                    if (settings != null)
                    {
                        rotation = settings.SampleRotation(rotation);
                    }

                    float clearance = GetClearance(position, rotation, prefab.transform.localScale, box, settings);
                    if (clearance < 0f)
                    {
                        continue;
                    }

                    if (clearance > bestClearance)
                    {
                        selected = index;
                        bestClearance = clearance;
                        pose = new Pose(position, rotation);
                    }

                    if (settings == null || !settings.AvoidBlockedFront || clearance >= settings.FrontDistance)
                    {
                        return GridSpawnReservation.TryReserveCell(index, kind, out reservationId);
                    }
                }
            }

            if (selected < 0)
            {
                return false;
            }

            usedFallback = true;
            return GridSpawnReservation.TryReserveCell(selected, kind, out reservationId);
        }

        private static float GetClearance(Vector3 position, Quaternion rotation, Vector3 scale,
            BoxCollider box, SpawnPlacementSettings settings)
        {
            if (settings == null || !settings.AvoidBlockedFront)
            {
                return float.MaxValue;
            }

            if (box == null || settings.LocalForward.sqrMagnitude < 0.001f)
            {
                return -1f;
            }

            Vector3 center = position + rotation * Vector3.Scale(box.center, scale);
            Vector3 half = Vector3.Scale(box.size * 0.5f, scale);
            half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
            half += Vector3.one * settings.CollisionPadding;
            if (Physics.CheckBox(center, half, rotation, settings.BlockingMask, QueryTriggerInteraction.Ignore))
            {
                return -1f;
            }

            Vector3 localForward = settings.LocalForward.normalized;
            Vector3 forward = rotation * localForward;
            float extent = Mathf.Abs(localForward.x) * half.x + Mathf.Abs(localForward.y) * half.y + Mathf.Abs(localForward.z) * half.z;
            Vector3 origin = center + forward * (extent + settings.CollisionPadding);
            Quaternion frontRotation = Quaternion.LookRotation(forward, rotation * Vector3.up);
            Vector3 frontHalf = new(settings.FrontHalfWidth, settings.FrontHalfHeight, settings.CollisionPadding);
            if (Physics.CheckBox(origin, frontHalf, frontRotation, settings.BlockingMask, QueryTriggerInteraction.Ignore))
            {
                return 0f;
            }

            if (Physics.BoxCast(origin, frontHalf, forward, out RaycastHit hit, frontRotation,
                settings.FrontDistance, settings.BlockingMask, QueryTriggerInteraction.Ignore))
            {
                return hit.distance;
            }

            return settings.FrontDistance;
        }
    }
}
