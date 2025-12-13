using UnityEngine;
using System.Collections.Generic;

public class ObstacleDetector
{
    private RoadAnalyzer analyzer;
    private bool enableDebug = true;

    public ObstacleDetector(RoadAnalyzer analyzer = null)
    {
        this.analyzer = analyzer;
    }

    public void DetectObstacles(WaypointSegment segment)
    {
        if (segment == null)
        {
            Debug.LogWarning("ObstacleDetector: segment is null");
            return;
        }

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer == -1)
        {
            Debug.LogWarning("ObstacleDetector: layer is null");
            segment.obstacleWidth = 0f;
            segment.obstacleLength = 0f;
            segment.obstaclePercent = 0f;
            return;
        }

        int obstacleLayerMask = 1 << obstacleLayer;

        FindObstaclesWithOverlapBox(segment, obstacleLayerMask);
    }

    private void FindObstaclesWithOverlapBox(WaypointSegment segment, int obstacleLayerMask)
    {
        Vector3 segmentStart = segment.segmentStart;
        Vector3 segmentEnd = segment.segmentEnd;
        Vector3 segmentDirection = segment.segmentDirection;
        float segmentLength = segment.segmentLength;
        float roadWidth = segment.roadWidth;

        Vector3 segmentCenter = (segmentStart + segmentEnd) / 2f;

        float boxLength = segmentLength + 1f;
        float boxWidth = roadWidth + 1f;
        float boxHeight = 5f; 

        Quaternion boxRotation = Quaternion.LookRotation(segmentDirection);

        if (enableDebug)
        {
            DrawDebugBox(segmentCenter, new Vector3(boxWidth / 2f, boxHeight / 2f, boxLength / 2f), boxRotation, Color.yellow, 100f);

            Vector3 rightDirectionA = Vector3.Cross(segmentDirection, Vector3.up).normalized;
            Vector3 leftEdgeStart = segmentStart - rightDirectionA * (roadWidth / 2f);
            Vector3 rightEdgeStart = segmentStart + rightDirectionA * (roadWidth / 2f);
            Vector3 leftEdgeEnd = segmentEnd - rightDirectionA * (roadWidth / 2f);
            Vector3 rightEdgeEnd = segmentEnd + rightDirectionA * (roadWidth / 2f);

            Debug.DrawLine(leftEdgeStart, leftEdgeEnd, Color.white, 100f);
            Debug.DrawLine(rightEdgeStart, rightEdgeEnd, Color.white, 100f);
        }

        Collider[] colliders = Physics.OverlapBox(
            segmentCenter,
            new Vector3(boxWidth / 2f, boxHeight / 2f, boxLength / 2f),
            boxRotation,
            obstacleLayerMask);

        if (colliders.Length == 0)
        {
            segment.obstacleWidth = 0f;
            segment.obstacleLength = 0f;
            segment.obstaclePercent = 0f;
            return;
        }

        List<Collider> validObstacles = new List<Collider>();
        Vector3 rightDirection = Vector3.Cross(segmentDirection, Vector3.up).normalized;

        foreach (Collider collider in colliders)
        {
            if (IsColliderInSegment(collider, segment))
            {
                validObstacles.Add(collider);

                if (enableDebug)
                {
                    Debug.DrawRay(collider.bounds.center, Vector3.up * 3f, Color.red, 100f);
                    DrawDebugBounds(collider.bounds, Color.magenta, 100f);
                }
            }
        }

        CalculateObstacleOccupancy(segment, validObstacles);
    }

    private bool IsColliderInSegment(Collider collider, WaypointSegment segment)
    {
        Bounds bounds = collider.bounds;
        Vector3 boundsCenter = bounds.center;
        Vector3 segmentStart = segment.segmentStart;
        Vector3 segmentDirection = segment.segmentDirection;
        float segmentLength = segment.segmentLength;
        float roadWidth = segment.roadWidth;
        Vector3 toCenter = boundsCenter - segmentStart;
        float projection = Vector3.Dot(toCenter, segmentDirection);
        if (projection < -1f || projection > segmentLength + 1f)
        {
            if (enableDebug)
            {
                Debug.DrawLine(boundsCenter, segmentStart + segmentDirection * projection, Color.gray, 1f);
            }
            return false;
        }
        Vector3 closestPointOnSegment = segmentStart + segmentDirection * Mathf.Clamp(projection, 0f, segmentLength);
        float distanceToSegmentLine = Vector3.Distance(boundsCenter, closestPointOnSegment);
        float colliderRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        bool isInRoad = distanceToSegmentLine - colliderRadius < roadWidth / 2f;
        if (enableDebug)
        {
            Color debugColor = isInRoad ? Color.green : Color.gray;
            Debug.DrawLine(boundsCenter, closestPointOnSegment, debugColor, 100f);
            Debug.DrawRay(closestPointOnSegment, Vector3.up * 1f, debugColor, 100f);
        }

        return isInRoad;
    }

    private void CalculateObstacleOccupancy(WaypointSegment segment, List<Collider> obstacles)
    {
        if (obstacles.Count == 0)
        {
            segment.obstacleWidth = 0f;
            segment.obstacleLength = 0f;
            segment.obstaclePercent = 0f;
            return;
        }

        Vector3 segmentStart = segment.segmentStart;
        Vector3 segmentDirection = segment.segmentDirection;
        Vector3 rightDirection = Vector3.Cross(segmentDirection, Vector3.up).normalized;
        float roadWidth = segment.roadWidth;
        float segmentLength = segment.segmentLength;

        List<Vector2> obstacleProjections = new List<Vector2>();

        foreach (Collider collider in obstacles)
        {
            List<Vector3> worldVertices = GetColliderWorldVertices(collider);
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            foreach (Vector3 vertex in worldVertices)
            {
                Vector3 toVertex = vertex - segmentStart;
                float z = Vector3.Dot(toVertex, segmentDirection);
                Vector3 vertexOnCenterLine = segmentStart + segmentDirection * z;
                Vector3 toVertexX = vertex - vertexOnCenterLine;
                float x = Vector3.Dot(toVertexX, rightDirection);

                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
            }
            obstacleProjections.Add(new Vector2(minX, maxX));
            obstacleProjections.Add(new Vector2(minZ, maxZ));
            if (enableDebug)
            {
                DrawOrientedProjection(segmentStart, segmentDirection, rightDirection,
                                     new Vector2(minX, maxX), new Vector2(minZ, maxZ),
                                     Color.magenta, 100f);
            }
        }
        obstacleProjections.Sort((a, b) => a.x.CompareTo(b.x));
        List<Vector2> mergedSegments = new List<Vector2>();

        if (obstacleProjections.Count > 0)
        {
            Vector2 current = obstacleProjections[0];

            for (int i = 1; i < obstacleProjections.Count; i++)
            {
                Vector2 next = obstacleProjections[i];
                if (next.x <= current.y)
                {
                    current.y = Mathf.Max(current.y, next.y);
                }
                else
                {
                    mergedSegments.Add(current);
                    current = next;
                }
            }

            mergedSegments.Add(current);
        }
        float totalOccupiedWidth = 0f;
        float totalOccupiedLength = 0f;

        foreach (Vector2 segmentZ in mergedSegments)
        {
            float minXInSegment = float.MaxValue;
            float maxXInSegment = float.MinValue;

            for (int i = 0; i < obstacles.Count; i++)
            {
                Vector2 widthProj = obstacleProjections[i * 2];
                Vector2 lengthProj = obstacleProjections[i * 2 + 1];
                if (!(lengthProj.y < segmentZ.x || lengthProj.x > segmentZ.y))
                {
                    minXInSegment = Mathf.Min(minXInSegment, widthProj.x);
                    maxXInSegment = Mathf.Max(maxXInSegment, widthProj.y);
                }
            }
            float segmentWidth = Mathf.Max(0, maxXInSegment - minXInSegment);
            segmentWidth = Mathf.Min(segmentWidth, roadWidth);
            float segmentLengthZ = Mathf.Max(0, segmentZ.y - segmentZ.x);
            segmentLengthZ = Mathf.Min(segmentLengthZ, segmentLength);
            if (segmentWidth > totalOccupiedWidth)
            {
                totalOccupiedWidth = segmentWidth;
            }

            totalOccupiedLength += segmentLengthZ;
            if (enableDebug && segmentWidth > 0)
            {
                Vector3 segStart = segmentStart + segmentDirection * segmentZ.x;
                Vector3 segEnd = segmentStart + segmentDirection * segmentZ.y;
                Vector3 center = (segStart + segEnd) / 2f;

                Debug.DrawLine(segStart, segEnd, new Color(1f, 0.5f, 0f, 1f), 100f);
                Debug.DrawLine(center - rightDirection * (segmentWidth / 2f),
                             center + rightDirection * (segmentWidth / 2f),
                             new Color(1f, 0.5f, 0f, 1f), 100f);
            }
        }
        segment.obstacleWidth = totalOccupiedWidth;
        segment.obstacleLength = Mathf.Min(totalOccupiedLength, segmentLength);
        segment.obstaclePercent = segment.obstacleWidth / segment.roadWidth * 100;
    }

    private List<Vector3> GetColliderWorldVertices(Collider collider)
    {
        List<Vector3> vertices = new List<Vector3>();

        if (collider is BoxCollider boxCollider)
        {
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localVertex = center + new Vector3(
                            x * size.x / 2f,
                            y * size.y / 2f,
                            z * size.z / 2f
                        );

                        Vector3 worldVertex = collider.transform.TransformPoint(localVertex);
                        vertices.Add(worldVertex);
                    }
                }
            }
        }
        else if (collider is SphereCollider sphereCollider)
        {
            Vector3 center = sphereCollider.center;
            float radius = sphereCollider.radius * Mathf.Max(
                collider.transform.lossyScale.x,
                collider.transform.lossyScale.y,
                collider.transform.lossyScale.z
            );

            Vector3 localSize = new Vector3(radius * 2f, radius * 2f, radius * 2f);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localVertex = center + new Vector3(
                            x * localSize.x / 2f,
                            y * localSize.y / 2f,
                            z * localSize.z / 2f
                        );

                        Vector3 worldVertex = collider.transform.TransformPoint(localVertex);
                        vertices.Add(worldVertex);
                    }
                }
            }
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            Vector3 center = capsuleCollider.center;
            float radius = capsuleCollider.radius;
            float height = capsuleCollider.height;

            Vector3 localSize = new Vector3(
                radius * 2f,
                height,
                radius * 2f
            );

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localVertex = center + new Vector3(
                            x * localSize.x / 2f,
                            y * localSize.y / 2f,
                            z * localSize.z / 2f
                        );

                        Vector3 worldVertex = collider.transform.TransformPoint(localVertex);
                        vertices.Add(worldVertex);
                    }
                }
            }
        }
        else
        {
            Bounds bounds = collider.bounds;
            vertices.Add(bounds.min);
            vertices.Add(bounds.max);
            vertices.Add(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
            vertices.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            vertices.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            vertices.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
            vertices.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
            vertices.Add(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
        }

        return vertices;
    }

    private void DrawOrientedProjection(Vector3 segmentStart, Vector3 segmentDirection, Vector3 rightDirection,
                                       Vector2 widthRange, Vector2 lengthRange, Color color, float duration)
    {
        Vector3[] corners = new Vector3[4];
        corners[0] = segmentStart +
                    segmentDirection * lengthRange.x +
                    rightDirection * widthRange.x;
        corners[1] = segmentStart +
                    segmentDirection * lengthRange.x +
                    rightDirection * widthRange.y;
        corners[2] = segmentStart +
                    segmentDirection * lengthRange.y +
                    rightDirection * widthRange.y;
        corners[3] = segmentStart +
                    segmentDirection * lengthRange.y +
                    rightDirection * widthRange.x;
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);
    }
    private void DrawDebugBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration)
    {
        Vector3[] corners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3 ext = new Vector3(
                (i & 1) == 0 ? -halfExtents.x : halfExtents.x,
                (i & 2) == 0 ? -halfExtents.y : halfExtents.y,
                (i & 4) == 0 ? -halfExtents.z : halfExtents.z
            );
            corners[i] = center + rotation * ext;
        }

        int[][] edges = new int[][]
        {
            new int[] {0,1}, new int[] {1,3}, new int[] {3,2}, new int[] {2,0},
            new int[] {4,5}, new int[] {5,7}, new int[] {7,6}, new int[] {6,4},
            new int[] {0,4}, new int[] {1,5}, new int[] {2,6}, new int[] {3,7}
        };

        foreach (var edge in edges)
        {
            Debug.DrawLine(corners[edge[0]], corners[edge[1]], color, duration);
        }
    }

    private void DrawDebugBounds(Bounds bounds, Color color, float duration)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            corners[i] = center + new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z
            );
        }

        int[][] edges = new int[][]
        {
            new int[] {0,1}, new int[] {1,3}, new int[] {3,2}, new int[] {2,0},
            new int[] {4,5}, new int[] {5,7}, new int[] {7,6}, new int[] {6,4},
            new int[] {0,4}, new int[] {1,5}, new int[] {2,6}, new int[] {3,7}
        };

        foreach (var edge in edges)
        {
            Debug.DrawLine(corners[edge[0]], corners[edge[1]], color, duration);
        }
    }
    public void SetDebugEnabled(bool enabled)
    {
        enableDebug = enabled;
    }
}