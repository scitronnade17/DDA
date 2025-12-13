using UnityEngine;

public class FrictionAnalyzer
{
    private RoadAnalyzer analyzer;

    public FrictionAnalyzer(RoadAnalyzer analyzer)
    {
        this.analyzer = analyzer;
    }

    public void DetermineFrictionFromPhysicMaterial(WaypointSegment segment)
    {
        Vector3 rayStart = segment.waypoint.position + Vector3.up * analyzer.raycastHeight;
     
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, analyzer.raycastDistance, analyzer.roadLayer))
        {
            Collider collider = hit.collider;

            if (collider != null)
            {
                PhysicMaterial physicMaterial = collider.sharedMaterial;
                segment.detectedPhysicMaterial = physicMaterial;

                if (physicMaterial != null)
                {
                    segment.frictionCoefficient = physicMaterial.dynamicFriction;
                }
                else
                {
                    segment.frictionCoefficient = analyzer.defaultFriction;
                }
            }
            else
            {
                segment.frictionCoefficient = analyzer.defaultFriction;
            }
        }
        else
        {
            segment.frictionCoefficient = analyzer.defaultFriction;
        }
    }
}