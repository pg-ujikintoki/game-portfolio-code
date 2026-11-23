using UnityEngine;

/// <summary>
/// 垂直な壁では上下、床と天井では左右に往復する。壁と床・天井のあいだは移らない。
/// </summary>
public class EnemyCrawler : WallEnemyBase
{
    private const float VerticalWallNormalThreshold = 0.7f;

    private enum PatrolAxis
    {
        None,
        Wall,
        Floor,
        Ceiling
    }

    public enum VerticalStartDirection
    {
        [InspectorName("上")]
        Up = 1,
        [InspectorName("下")]
        Down = -1
    }

    [Header("Crawler Patrol")]
    [Tooltip("壁では初期位置から上下へ、床と天井では左右へ進む距離。端か面の途切れで折り返す。")]
    [SerializeField, Min(0.01f)] private float patrolDistance = 2f;
    [Tooltip("最初に進む方向。壁では上下。床と天井では上を右、下を左にする。")]
    [SerializeField] private VerticalStartDirection startDirection = VerticalStartDirection.Up;

    private float originX;
    private float originY;
    private int patrolDirection = 1;
    private bool hasPatrolOrigin;
    private PatrolAxis patrolAxis = PatrolAxis.Wall;

    protected override void OnSurfaceCrawlBegan()
    {
        originX = transform.position.x;
        originY = transform.position.y;
        hasPatrolOrigin = true;
        patrolDirection = startDirection == VerticalStartDirection.Down ? -1 : 1;
        patrolAxis = PatrolAxis.Wall;
    }

    protected override void OnInitialSurfaceResolved(Vector2 surfaceNormal, bool foundSurface)
    {
        if (!foundSurface)
        {
            patrolAxis = PatrolAxis.None;
            return;
        }

        if (Mathf.Abs(surfaceNormal.x) >= VerticalWallNormalThreshold)
        {
            patrolAxis = PatrolAxis.Wall;
            return;
        }

        if (surfaceNormal.y <= -VerticalWallNormalThreshold)
        {
            patrolAxis = PatrolAxis.Ceiling;
            return;
        }

        if (surfaceNormal.y >= VerticalWallNormalThreshold)
        {
            patrolAxis = PatrolAxis.Floor;
            return;
        }

        patrolAxis = PatrolAxis.None;
    }

    protected override void UpdateTravelConstraints()
    {
        if (IsHorizontalAxis)
        {
            float x = rb != null ? rb.position.x : transform.position.x;
            if (patrolDirection > 0 && x >= originX + patrolDistance)
            {
                patrolDirection = -1;
            }
            else if (patrolDirection < 0 && x <= originX - patrolDistance)
            {
                patrolDirection = 1;
            }

            return;
        }

        if (patrolAxis != PatrolAxis.Wall)
        {
            return;
        }

        float y = rb != null ? rb.position.y : transform.position.y;
        if (patrolDirection > 0 && y >= originY + patrolDistance)
        {
            patrolDirection = -1;
        }
        else if (patrolDirection < 0 && y <= originY - patrolDistance)
        {
            patrolDirection = 1;
        }
    }

    protected override bool ReversesWhenPathEnds => patrolAxis != PatrolAxis.None;

    protected override bool AllowsSurface(Vector2 normal)
    {
        if (patrolAxis == PatrolAxis.Ceiling)
        {
            return normal.y <= -VerticalWallNormalThreshold;
        }

        if (patrolAxis == PatrolAxis.Floor)
        {
            return normal.y >= VerticalWallNormalThreshold;
        }

        if (patrolAxis == PatrolAxis.Wall)
        {
            return Mathf.Abs(normal.x) >= VerticalWallNormalThreshold;
        }

        return false;
    }

    protected override int ResolveCrawlSign(Vector2 up)
    {
        if (IsHorizontalAxis)
        {
            return SignForHorizontalTravel(up, patrolDirection);
        }

        return SignForVerticalTravel(up, patrolDirection);
    }

    protected override void OnPathEndReversed(int newSign, Vector2 surfaceNormal)
    {
        Vector2 tangent = new Vector2(surfaceNormal.y, -surfaceNormal.x);
        Vector2 travel = tangent * newSign;
        float travelAlongAxis = IsHorizontalAxis ? travel.x : travel.y;
        if (Mathf.Abs(travelAlongAxis) <= 0.0001f)
        {
            patrolDirection = -patrolDirection;
            return;
        }

        patrolDirection = travelAlongAxis > 0f ? 1 : -1;
    }

    private static int SignForVerticalTravel(Vector2 surfaceNormal, int worldVerticalDirection)
    {
        Vector2 tangent = new Vector2(surfaceNormal.y, -surfaceNormal.x);
        if (Mathf.Abs(tangent.y) <= 0.0001f)
        {
            return worldVerticalDirection >= 0 ? 1 : -1;
        }

        bool positiveTangentGoesUp = tangent.y > 0f;
        bool wantUp = worldVerticalDirection >= 0;
        return positiveTangentGoesUp == wantUp ? 1 : -1;
    }

    private static int SignForHorizontalTravel(Vector2 surfaceNormal, int worldHorizontalDirection)
    {
        Vector2 tangent = new Vector2(surfaceNormal.y, -surfaceNormal.x);
        if (Mathf.Abs(tangent.x) <= 0.0001f)
        {
            return worldHorizontalDirection >= 0 ? 1 : -1;
        }

        bool positiveTangentGoesRight = tangent.x > 0f;
        bool wantRight = worldHorizontalDirection >= 0;
        return positiveTangentGoesRight == wantRight ? 1 : -1;
    }

    private bool IsHorizontalAxis => patrolAxis == PatrolAxis.Floor || patrolAxis == PatrolAxis.Ceiling;

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        bool horizontal = IsHorizontalAxis
            || (!Application.isPlaying && Mathf.Abs(transform.up.y) >= VerticalWallNormalThreshold);

        Gizmos.color = new Color(1f, 0.45f, 0.8f);
        if (horizontal)
        {
            float centerX = hasPatrolOrigin ? originX : position.x;
            Vector3 left = new Vector3(centerX - patrolDistance, position.y, position.z);
            Vector3 right = new Vector3(centerX + patrolDistance, position.y, position.z);
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left + Vector3.down * 0.25f, left + Vector3.up * 0.25f);
            Gizmos.DrawLine(right + Vector3.down * 0.25f, right + Vector3.up * 0.25f);
            return;
        }

        float centerY = hasPatrolOrigin ? originY : position.y;
        Vector3 bottom = new Vector3(position.x, centerY - patrolDistance, position.z);
        Vector3 top = new Vector3(position.x, centerY + patrolDistance, position.z);
        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawLine(bottom + Vector3.left * 0.25f, bottom + Vector3.right * 0.25f);
        Gizmos.DrawLine(top + Vector3.left * 0.25f, top + Vector3.right * 0.25f);
    }
}
