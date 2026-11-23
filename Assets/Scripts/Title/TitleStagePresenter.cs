using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// タイトルとエンディングの背景にステージを生成し、開始地点へプレイヤーを置く。
/// カメラは Stage01 と同じく、画面が LevelBounds からはみ出さない位置に置く。
/// 体力表示などの戦闘 UI は作らない。
/// </summary>
public class TitleStagePresenter : MonoBehaviour
{
    private const string PlayerSpawnPointName = "PlayerSpawnPoint";
    private const string LevelBoundsName = "LevelBounds";
    private const float CameraDistance = 10f;

    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float orthographicSize = 3.5f;

    private Camera mainCamera;
    private Transform followTarget;
    private Collider2D levelBounds;
    private Vector3 spawnPosition;
    private readonly List<Vector2> levelOutline = new List<Vector2>();

    private void Awake()
    {
        mainCamera = Camera.main;
        PrepareCamera(mainCamera);

        if (stagePrefab == null)
        {
            Debug.LogError("[TitleStagePresenter] Stage prefab is not assigned.");
            return;
        }

        GameObject stageInstance = Instantiate(stagePrefab);
        stageInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        stageInstance.transform.localScale = Vector3.one;
        DisableTitleOnlyBehaviours(stageInstance);

        Transform spawnPoint = FindChildTransformByName(stageInstance.transform, PlayerSpawnPointName);
        spawnPosition = spawnPoint != null ? spawnPoint.position : ResolveFallbackSpawn(stageInstance);
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        followTarget = spawnPoint;
        if (playerPrefab != null)
        {
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            PlayerController player = playerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.DisableControl();
                followTarget = player.transform;
            }
            else
            {
                followTarget = playerObject.transform;
            }
        }
        else
        {
            Debug.LogWarning("[TitleStagePresenter] Player prefab is not assigned.");
        }

        levelBounds = FindChildColliderByName(stageInstance.transform, LevelBoundsName);
        ApplyCameraPosition();
    }

    private void LateUpdate()
    {
        ApplyCameraPosition();
    }

    /// <summary>アクションステージと同じ正射影サイズで背景を映す。</summary>
    private void PrepareCamera(Camera cameraToPrepare)
    {
        if (cameraToPrepare == null)
        {
            return;
        }

        PixelPerfectCamera pixelPerfect = cameraToPrepare.GetComponent<PixelPerfectCamera>();
        if (pixelPerfect != null)
        {
            pixelPerfect.enabled = false;
        }

        cameraToPrepare.orthographic = true;
        cameraToPrepare.orthographicSize = orthographicSize;
        cameraToPrepare.clearFlags = CameraClearFlags.SolidColor;
    }

    /// <summary>タイトルではヘルプ演出を出さない。</summary>
    private static void DisableTitleOnlyBehaviours(GameObject stageInstance)
    {
        HelpTrigger[] helpTriggers = stageInstance.GetComponentsInChildren<HelpTrigger>(true);
        for (int i = 0; i < helpTriggers.Length; i++)
        {
            helpTriggers[i].enabled = false;
        }
    }

    private Vector3 ResolveFallbackSpawn(GameObject stageInstance)
    {
        StageDefinition stage = stageInstance.GetComponent<StageDefinition>();
        if (stage == null)
        {
            return Vector3.zero;
        }

        return new Vector3(stage.PlayerStartPosition.x, stage.PlayerStartPosition.y, 0f);
    }

    /// <summary>
    /// プレイヤーを画面中心に置くと左と下の境界からはみ出す。
    /// アクションステージの Confiner と同じく、画面全体が LevelBounds に収まる位置へずらす。
    /// </summary>
    private void ApplyCameraPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = orthographicSize;

        Vector3 focus = followTarget != null ? followTarget.position : spawnPosition;
        EnsureLevelOutline(focus);
        Vector2 confined = ConfineCamera(focus, levelOutline, orthographicSize, mainCamera.aspect);
        mainCamera.transform.position = new Vector3(confined.x, confined.y, -CameraDistance);
    }

    private void EnsureLevelOutline(Vector3 focus)
    {
        if (levelOutline.Count >= 3 || levelBounds == null)
        {
            return;
        }

        if (!TryGetWorldOutline(levelBounds, focus, out List<Vector2> outline))
        {
            return;
        }

        levelOutline.AddRange(outline);
    }

    private static Vector2 ConfineCamera(Vector3 focus, List<Vector2> outline, float halfHeight, float aspect)
    {
        Vector2 cameraPosition = focus;
        if (outline == null || outline.Count < 3 || halfHeight <= 0f)
        {
            return cameraPosition;
        }

        float safeAspect = aspect > 0.01f ? aspect : 16f / 9f;
        float halfWidth = halfHeight * safeAspect;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            Vector2 push = Vector2.zero;
            Vector2[] corners =
            {
                cameraPosition + new Vector2(-halfWidth, -halfHeight),
                cameraPosition + new Vector2(halfWidth, -halfHeight),
                cameraPosition + new Vector2(halfWidth, halfHeight),
                cameraPosition + new Vector2(-halfWidth, halfHeight),
            };

            bool anyOutside = false;
            for (int i = 0; i < corners.Length; i++)
            {
                if (ContainsPoint(outline, corners[i]))
                {
                    continue;
                }

                anyOutside = true;
                Vector2 closest = ClosestPointOnOutline(outline, corners[i]);
                Vector2 delta = closest - corners[i];
                if (Mathf.Abs(delta.x) > Mathf.Abs(push.x))
                {
                    push.x = delta.x;
                }

                if (Mathf.Abs(delta.y) > Mathf.Abs(push.y))
                {
                    push.y = delta.y;
                }
            }

            if (!anyOutside || push.sqrMagnitude < 0.000001f)
            {
                break;
            }

            cameraPosition += push;
        }

        return cameraPosition;
    }

    private static bool TryGetWorldOutline(Collider2D bounds, Vector3 focus, out List<Vector2> outline)
    {
        outline = null;
        CompositeCollider2D composite = bounds as CompositeCollider2D;
        if (composite == null || composite.pathCount <= 0 || composite.pointCount <= 0)
        {
            return false;
        }

        var buffer = new Vector2[composite.pointCount];
        List<Vector2> containingPath = null;
        List<Vector2> largestPath = null;
        for (int pathIndex = 0; pathIndex < composite.pathCount; pathIndex++)
        {
            int pointCount = composite.GetPath(pathIndex, buffer);
            if (pointCount < 3)
            {
                continue;
            }

            var worldPath = new List<Vector2>(pointCount);
            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                worldPath.Add(composite.transform.TransformPoint(buffer[pointIndex]));
            }

            if (largestPath == null || worldPath.Count > largestPath.Count)
            {
                largestPath = worldPath;
            }

            if (ContainsPoint(worldPath, focus))
            {
                containingPath = worldPath;
                break;
            }
        }

        outline = containingPath ?? largestPath;
        return outline != null && outline.Count >= 3;
    }

    private static bool ContainsPoint(List<Vector2> outline, Vector2 point)
    {
        bool inside = false;
        int previousIndex = outline.Count - 1;
        for (int index = 0; index < outline.Count; index++)
        {
            Vector2 current = outline[index];
            Vector2 previous = outline[previousIndex];
            bool crosses = (current.y > point.y) != (previous.y > point.y);
            if (crosses)
            {
                float intersectionX = previous.x + (point.y - previous.y) * (current.x - previous.x) / (current.y - previous.y);
                if (point.x < intersectionX)
                {
                    inside = !inside;
                }
            }

            previousIndex = index;
        }

        return inside;
    }

    private static Vector2 ClosestPointOnOutline(List<Vector2> outline, Vector2 point)
    {
        Vector2 closest = outline[0];
        float closestDistance = float.MaxValue;
        for (int index = 0; index < outline.Count; index++)
        {
            Vector2 start = outline[index];
            Vector2 end = outline[(index + 1) % outline.Count];
            Vector2 candidate = ClosestPointOnSegment(start, end, point);
            float distance = (candidate - point).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 start, Vector2 end, Vector2 point)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.000001f)
        {
            return start;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return start + segment * t;
    }

    private static Transform FindChildTransformByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Collider2D FindChildColliderByName(Transform root, string objectName)
    {
        Transform target = FindChildTransformByName(root, objectName);
        return target != null ? target.GetComponent<Collider2D>() : null;
    }
}
