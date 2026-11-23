using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Game.Items;
using TMPro;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject readyText;
    [SerializeField] private GameObject warningText;
    [SerializeField] private GameObject clearText;

    [Header("Scene References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject boss;
    [SerializeField] private Collider2D bossTriggerZone;
    [SerializeField] private Transform stageContentRoot;

    [Header("Boss Camera")]
    [SerializeField] private float bossCameraMoveSmoothTime = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip stageBgm;
    [SerializeField] private AudioClip bossBgm;
    [SerializeField] private AudioClip stageClearSfx;
    [SerializeField, Min(0f)] private float stageClearSceneDelay = 3f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float stageClearSfxVolume = 0.9f;

    private TMP_Text readyTextComponent;
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private bool bossStarted = false;
    private bool stageClearStarted = false;
    private GameObject loadedMapInstance;
    private Transform stagePlayerSpawnPoint;
    private Transform stageBossSpawnPoint;
    private Collider2D stageMainBoundsCollider;
    private Collider2D bossRoomBoundsCollider;
    private Transform bossCameraAnchor;
    private Vector3 bossCameraDestination;
    private Vector3 bossCameraVelocity;
    private bool bossCameraMoving;

    void Start()
    {
        ResolveSceneReferences();
        readyTextComponent = readyText != null ? readyText.GetComponent<TMP_Text>() : null;

        LoadCurrentStageData();
        BindVirtualCamerasToPlayer();
        BindCameraConfinersToLevelBounds();
        EnsureAudioSources();
        PlayStageBgm();
        StartCoroutine(StageStartSequence());
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        var playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            return;
        }

        if (!bossStarted && bossTriggerZone != null && bossTriggerZone.IsTouching(playerCollider))
        {
            player.rb.linearVelocity = Vector2.zero;
            StartCoroutine(BossStartSequence());
        }
    }

    void LateUpdate()
    {
        if (!bossCameraMoving || bossCameraAnchor == null)
        {
            return;
        }

        float smoothTime = Mathf.Max(0f, bossCameraMoveSmoothTime);
        if (smoothTime <= 0f)
        {
            bossCameraAnchor.position = bossCameraDestination;
            bossCameraMoving = false;
            bossCameraVelocity = Vector3.zero;
            return;
        }

        Vector3 nextPosition = Vector3.SmoothDamp(
            bossCameraAnchor.position,
            bossCameraDestination,
            ref bossCameraVelocity,
            smoothTime);
        nextPosition.z = bossCameraDestination.z;
        bossCameraAnchor.position = nextPosition;

        if ((nextPosition - bossCameraDestination).sqrMagnitude <= 0.0001f)
        {
            bossCameraAnchor.position = bossCameraDestination;
            bossCameraMoving = false;
            bossCameraVelocity = Vector3.zero;
        }
    }

    IEnumerator StageStartSequence()
    {
        if (player != null)
        {
            player.DisableControl();
            player.enabled = false;
        }

        if (readyText != null)
        {
            readyText.SetActive(true);
        }

        if (readyTextComponent != null)
        {
            readyTextComponent.text = ResolveReadyText();
        }

        yield return new WaitForSeconds(2f);

        if (readyText != null)
        {
            readyText.SetActive(false);
        }

        if (player != null)
        {
            player.enabled = true;
            player.EnableControl();
        }
    }

    IEnumerator BossStartSequence()
    {
        bossStarted = true;

        PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
        {
            pauseManager.SetPauseInputLocked(true);
        }

        if (player != null)
        {
            player.BeginBossEntryLock();
        }

        ClearFieldForBossEntry();

        BossDoor door = bossTriggerZone != null ? bossTriggerZone.GetComponent<BossDoor>() : null;
        if (door != null)
        {
            yield return door.Raise();
            yield return WaitUntilPlayerGrounded();
            yield return WalkPlayerIntoBossRoom(door.PlayerWalkDistance);
        }

        LockCameraToBossRoom();

        if (door != null)
        {
            yield return door.Lower();
        }

        if (warningText != null)
        {
            warningText.SetActive(true);
        }

        PlayBossBgm();

        yield return new WaitForSeconds(2f);

        if (warningText != null)
        {
            warningText.SetActive(false);
        }

        if (boss != null)
        {
            boss.SetActive(true);

            var bossController = boss.GetComponent<BossBase>();
            if (bossController != null)
            {
                bossController.enabled = true;
            }

            var enemyBase = boss.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.NotifyBossAppeared();
            }
        }

        if (player != null)
        {
            player.EndBossEntryLock();
        }

        if (pauseManager != null)
        {
            pauseManager.SetPauseInputLocked(false);
        }
    }

    private IEnumerator WaitUntilPlayerGrounded()
    {
        if (player == null)
        {
            yield break;
        }

        player.BeginScriptedFall();
        while (player != null && !player.isGrounded)
        {
            yield return null;
        }

        if (player != null)
        {
            player.EndScriptedFall();
        }
    }

    private IEnumerator WalkPlayerIntoBossRoom(float distance)
    {
        if (player == null || distance <= 0f || player.walkSpeed <= 0f)
        {
            yield break;
        }

        player.BeginScriptedWalk();
        float startX = player.transform.position.x;
        float lastX = startX;
        float stuckTime = 0f;
        while (player != null && player.transform.position.x - startX < distance)
        {
            yield return null;
            if (player == null)
            {
                yield break;
            }

            float x = player.transform.position.x;
            if (x <= lastX + 0.001f)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime >= 0.5f)
                {
                    break;
                }
            }
            else
            {
                stuckTime = 0f;
                lastX = x;
            }
        }

        if (player != null)
        {
            player.EndScriptedWalk();
        }
    }

    private void ClearFieldForBossEntry()
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
            {
                spawners[i].StopSpawning();
            }
        }

        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !enemies[i].IsBoss)
            {
                Destroy(enemies[i].gameObject);
            }
        }

        DestroyComponents<EnemyBullet>();
        DestroyComponents<EnemyCannonball>();
        DestroyComponents<PunchedCorpse>();
        DestroyPickups();
    }

    private void DestroyPickups()
    {
        PickupItem[] pickups = FindObjectsByType<PickupItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var roots = new HashSet<GameObject>();
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] == null)
            {
                continue;
            }

            roots.Add(ResolvePickupRoot(pickups[i].transform).gameObject);
        }

        foreach (GameObject root in roots)
        {
            if (root != null)
            {
                Destroy(root);
            }
        }
    }

    private Transform ResolvePickupRoot(Transform target)
    {
        Transform stageRoot = loadedMapInstance != null ? loadedMapInstance.transform : null;
        while (target.parent != null && target.parent != stageRoot && target.parent != stageContentRoot)
        {
            target = target.parent;
        }

        return target;
    }

    private static void DestroyComponents<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                Destroy(components[i].gameObject);
            }
        }
    }

    public void OnBossDefeated()
    {
        StopBgm();
        StageClear();
    }

    private void StageClear()
    {
        if (stageClearStarted)
        {
            return;
        }

        stageClearStarted = true;
        PlayStageClearSfx();

        if (clearText != null)
        {
            clearText.SetActive(true);
        }

        if (player != null)
        {
            player.isControlLockedStage = true;
            player.rb.linearVelocity = Vector2.zero;
        }

        StartCoroutine(WaitAfterClear());
    }

    private IEnumerator WaitAfterClear()
    {
        if (stageClearSceneDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(stageClearSceneDelay);
        }

        SceneLoader.Load("EndingScene");
    }

    private void LoadCurrentStageData()
    {
        ResolveSceneReferences();
        bossStarted = false;
        stageClearStarted = false;
        bossTriggerZone = null;
        stageMainBoundsCollider = null;
        bossRoomBoundsCollider = null;
        stagePlayerSpawnPoint = null;

        var progress = GameProgressManager.Instance;
        var stage = progress != null ? progress.CurrentStage : null;
        if (stage == null)
        {
            return;
        }

        if (stage.StagePrefab != null)
        {
            if (loadedMapInstance != null)
            {
                Destroy(loadedMapInstance);
            }

            Transform root = stageContentRoot != null ? stageContentRoot : transform;
            loadedMapInstance = Instantiate(stage.StagePrefab, root);
            loadedMapInstance.transform.localPosition = Vector3.zero;
            loadedMapInstance.transform.localRotation = Quaternion.identity;
            loadedMapInstance.transform.localScale = Vector3.one;
            stagePlayerSpawnPoint = FindChildTransformByName(loadedMapInstance.transform, "PlayerSpawnPoint");
            stageBossSpawnPoint = FindChildTransformByName(loadedMapInstance.transform, "BossSpawnPoint");
            bossTriggerZone = FindChildColliderByName(loadedMapInstance.transform, "BossDoor");
            stageMainBoundsCollider = FindChildColliderByName(loadedMapInstance.transform, "LevelBounds");
            bossRoomBoundsCollider = FindChildColliderByName(loadedMapInstance.transform, "BossRoomLevelBounds");
        }
        else
        {
            stageBossSpawnPoint = null;
            stagePlayerSpawnPoint = null;
        }

        if (player != null)
        {
            Vector3 spawnPosition = player.transform.position;
            Quaternion spawnRotation = player.transform.rotation;

            if (stagePlayerSpawnPoint != null)
            {
                spawnPosition = stagePlayerSpawnPoint.position;
                spawnRotation = stagePlayerSpawnPoint.rotation;
            }
            else
            {
                spawnPosition = new Vector3(stage.PlayerStartPosition.x, stage.PlayerStartPosition.y, player.transform.position.z);
            }

            player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            if (player.rb != null)
            {
                player.rb.linearVelocity = Vector2.zero;
            }
        }

        if (boss != null)
        {
            Destroy(boss);
            boss = null;
        }

        if (stage.BossPrefab != null)
        {
            Vector3 spawnPosition = stageBossSpawnPoint != null
                ? stageBossSpawnPoint.position
                : Vector3.zero;
            Quaternion spawnRotation = stageBossSpawnPoint != null
                ? stageBossSpawnPoint.rotation
                : Quaternion.identity;
            Transform bossParent = stageContentRoot != null ? stageContentRoot : transform;

            boss = Instantiate(stage.BossPrefab, spawnPosition, spawnRotation, bossParent);
            boss.SetActive(false);

            var enemyBase = boss.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.SetBossFlag(true);
            }

            var bossController = boss.GetComponent<BossBase>();
            if (bossController != null)
            {
                bossController.enabled = false;
            }
        }
    }

    private string ResolveReadyText()
    {
        if (GameProgressManager.Instance == null)
        {
            return "Debug Mode";
        }

        return $"{GameProgressManager.Instance.GetCurrentStageLabel()} READY";
    }

    private void BindVirtualCamerasToPlayer()
    {
        ResolveSceneReferences();

        if (player == null)
        {
            return;
        }

        SetVirtualCameraFollow(player.transform);
    }

    /// <summary>
    /// ボス戦闘中はカメラを止め、BossRoomLevelBounds を現在の表示サイズに一致させる。
    /// 左下は配置済みの部屋を維持し、幅と高さだけを画面に合わせる。
    /// 表示サイズと同じ範囲では Confiner の縮小領域が潰れて追従が残るため、中心の固定ターゲットで止める。
    /// ターゲットは現在のカメラ位置から中心へ滑らかに進み、到着後は動かない。
    /// </summary>
    private void LockCameraToBossRoom()
    {
        if (!TryFitBossRoomToCameraView(out Vector2 roomCenter))
        {
            return;
        }

        if (bossCameraAnchor == null)
        {
            var anchorObject = new GameObject("BossCameraAnchor");
            bossCameraAnchor = anchorObject.transform;
        }

        Vector3 cameraPosition = ResolveVirtualCameraPosition();
        bossCameraAnchor.SetParent(bossRoomBoundsCollider.transform, false);
        bossCameraAnchor.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
        bossCameraDestination = new Vector3(roomCenter.x, roomCenter.y, 0f);
        bossCameraVelocity = Vector3.zero;
        bossCameraMoving = true;
        SetVirtualCameraFollow(bossCameraAnchor);
        ClearCameraConfiners();
    }

    private Vector3 ResolveVirtualCameraPosition()
    {
        var virtualCameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null && virtualCameras[i].m_Lens.Orthographic)
            {
                return virtualCameras[i].transform.position;
            }
        }

        return Camera.main != null ? Camera.main.transform.position : Vector3.zero;
    }

    private bool TryFitBossRoomToCameraView(out Vector2 worldCenter)
    {
        worldCenter = default;
        if (bossRoomBoundsCollider == null)
        {
            return false;
        }

        float viewHeight = ResolveCameraViewHeight();
        float viewWidth = viewHeight * ResolveCameraAspect();
        if (viewHeight <= 0f || viewWidth <= 0f)
        {
            return false;
        }

        Vector2 worldMin = bossRoomBoundsCollider.bounds.min;
        Vector2 worldMax = worldMin + new Vector2(viewWidth, viewHeight);
        worldCenter = (worldMin + worldMax) * 0.5f;

        var boxes = bossRoomBoundsCollider.GetComponentsInChildren<BoxCollider2D>(true);
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == null)
            {
                continue;
            }

            if (i == 0)
            {
                ApplyWorldRectToBox(boxes[i], worldMin, worldMax);
            }
            else
            {
                boxes[i].enabled = false;
            }
        }

        return true;
    }

    private static void ApplyWorldRectToBox(BoxCollider2D box, Vector2 worldMin, Vector2 worldMax)
    {
        Transform boxTransform = box.transform;
        float z = boxTransform.position.z;
        Vector3 localMin = boxTransform.InverseTransformPoint(new Vector3(worldMin.x, worldMin.y, z));
        Vector3 localMax = boxTransform.InverseTransformPoint(new Vector3(worldMax.x, worldMax.y, z));
        box.offset = (Vector2)((localMin + localMax) * 0.5f);
        box.size = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));
    }

    private float ResolveCameraViewHeight()
    {
        var virtualCameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null && virtualCameras[i].m_Lens.Orthographic)
            {
                return virtualCameras[i].m_Lens.OrthographicSize * 2f;
            }
        }

        return 7f;
    }

    private static float ResolveCameraAspect()
    {
        if (Camera.main != null && Camera.main.aspect > 0f)
        {
            return Camera.main.aspect;
        }

        return 16f / 9f;
    }

    private void SetVirtualCameraFollow(Transform target)
    {
        if (target == null)
        {
            return;
        }

        var virtualCameras = FindObjectsOfType<CinemachineVirtualCameraBase>(true);
        foreach (var virtualCamera in virtualCameras)
        {
            if (virtualCamera == null)
            {
                continue;
            }

            virtualCamera.Follow = target;

            if (virtualCamera.LookAt == null || (player != null && virtualCamera.LookAt == player.transform))
            {
                virtualCamera.LookAt = target;
            }
        }
    }

    private static void ClearCameraConfiners()
    {
        var confiners = FindObjectsOfType<CinemachineConfiner2D>(true);
        foreach (var confiner in confiners)
        {
            if (confiner == null)
            {
                continue;
            }

            confiner.m_BoundingShape2D = null;
            confiner.m_Damping = 0f;
            confiner.InvalidateCache();
        }
    }

    /// <summary>
    /// ステージのプレハブは実行時に生成されるため、エディター上では Confiner は LevelBounds を参照できない。
    /// 生成された LevelBounds のコライダーに CinemachineConfiner2D を接続する（トリガーでも拘束可能）。
    /// ボス部屋: オプションの BossRoomLevelBounds オブジェクト — LevelBounds と同じセットアップ（CompositeCollider2D + Static Rigidbody2D + 子 BoxCollider2D をマージ）。
    /// CinemachineConfiner2D は PolygonCollider2D および CompositeCollider2D をベイクする（子のマージされたボックスではなく、コンポジットのルートを割り当てる）。
    /// </summary>
    private void BindCameraConfinersToLevelBounds()
    {
        BindCameraConfinersToCollider(stageMainBoundsCollider);
    }

    private void BindCameraConfinersToCollider(Collider2D boundsCollider)
    {
        if (boundsCollider == null)
        {
            return;
        }

        var confiners = FindObjectsOfType<CinemachineConfiner2D>(true);
        foreach (var confiner in confiners)
        {
            if (confiner == null)
            {
                continue;
            }

            confiner.m_BoundingShape2D = boundsCollider;
            confiner.InvalidateCache();
        }
    }

    /// <summary>ステージ開始時の BGM をループ再生する。ステージ定義に曲があればそちらを使う。</summary>
    private void PlayStageBgm()
    {
        StageDefinition stage = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.CurrentStage
            : null;
        AudioClip clip = stage != null && stage.Bgm != null ? stage.Bgm : stageBgm;
        PlayBgm(clip);
    }

    /// <summary>WARNING 表示と同時にボス用 BGM へ切り替える。</summary>
    private void PlayBossBgm()
    {
        PlayBgm(bossBgm);
    }

    private void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>再生中の BGM を止める。</summary>
    private void StopBgm()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
    }

    private void PlayStageClearSfx()
    {
        if (sfxSource == null || stageClearSfx == null)
        {
            return;
        }

        sfxSource.PlayOneShot(stageClearSfx, stageClearSfxVolume);
    }

    private void EnsureAudioSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void ResolveSceneReferences()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        }
    }

    private static Transform FindChildTransformByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (var targetTransform in root.GetComponentsInChildren<Transform>(true))
        {
            if (targetTransform != null && targetTransform.name == objectName)
            {
                return targetTransform;
            }
        }

        return null;
    }

    private static Collider2D FindChildColliderByName(Transform root, string objectName)
    {
        var targetTransform = FindChildTransformByName(root, objectName);
        return targetTransform != null ? targetTransform.GetComponent<Collider2D>() : null;
    }
}
