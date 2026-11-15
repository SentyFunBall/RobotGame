using System.Collections;
using UnityEngine;

public class LevelDirector : MonoBehaviour
{
    private enum LevelState
    {
        Stage1,
        Shop1,
        Stage2,
        Shop2,
    }

    private static LevelState currentLevelState = LevelState.Stage1;
    private static GameObject player;
    private static DungeonGenerator dungeonGenerator;

    [SerializeField]
    private AudioClip bossStart;

    private DungeonRoom startedBossRoom;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        dungeonGenerator = FindAnyObjectByType<DungeonGenerator>();
        if (dungeonGenerator == null)
            Debug.LogError("DungeonGenerator not found in the scene.");

        if (player == null)
            Debug.LogError("Player not found in the scene.");

        if (dungeonGenerator != null)
        {
            dungeonGenerator.Reset();
            dungeonGenerator.GenerateDungeon(currentLevelState == LevelState.Stage2);
        }

        MusicManager musicManager = FindAnyObjectByType<MusicManager>();
        if (musicManager != null)
        {
            musicManager.ChangeVolume(0.2f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
            return;

        // When player gets close to end room of stage 1, show menu screen
        if (
            DungeonGenerator.EndRoom != null
            && Vector3.Distance(
                player.transform.position,
                new Vector3(
                    DungeonGenerator.EndRoom.GridPosition.x,
                    1f,
                    DungeonGenerator.EndRoom.GridPosition.z
                )
            ) < 3f
            && currentLevelState == LevelState.Stage1
        )
        {
            AdvanceToShop();
        }

        // Show boss UI when player enters the boss room
        if (
            DungeonGenerator.EndRoom != null
            && Vector3.Distance(
                player.transform.position,
                new Vector3(
                    DungeonGenerator.EndRoom.GridPosition.x,
                    1f,
                    DungeonGenerator.EndRoom.GridPosition.z
                )
            ) < 15f
            && currentLevelState == LevelState.Stage2
        )
        {
            GameMenusController menus = FindAnyObjectByType<GameMenusController>();
            if (menus != null)
            {
                if (!menus.IsBossUIActive())
                {
                    menus.ShowBossUI();
                }
            }
        }

        if (
            currentLevelState == LevelState.Stage2
            && DungeonGenerator.EndRoom != null
            && DungeonGenerator.EndRoom != startedBossRoom
            && IsPlayerInsideBossRoom()
        )
        {
            startedBossRoom = DungeonGenerator.EndRoom;
            StartCoroutine(StartBossFight(startedBossRoom));
        }
    }

    private static bool IsPlayerInsideBossRoom()
    {
        if (player == null || dungeonGenerator == null || DungeonGenerator.EndRoom == null)
            return false;

        float halfSize = dungeonGenerator.Difficulty.Size * 0.5f - 1f;
        Vector3 position = player.transform.position;
        Vector3Int center = DungeonGenerator.EndRoom.GridPosition;
        return Mathf.Abs(position.x - center.x) < halfSize
            && Mathf.Abs(position.z - center.z) < halfSize;
    }

    private IEnumerator StartBossFight(DungeonRoom room)
    {
        if (bossStart != null)
            AudioSource.PlayClipAtPoint(bossStart, player.transform.position);
        else
            Debug.LogError("Boss start sound is not assigned.", this);

        yield return new WaitForSeconds(1.5f);
        yield return new WaitUntil(() => room == null || IsPlayerInsideBossRoom());

        if (room == null || room != DungeonGenerator.EndRoom)
            yield break;

        if (room.BossFloor != null)
            Destroy(room.BossFloor);
        else
            Debug.LogError("Boss room floor is not assigned.", room);

        if (room.BossLock != null)
            room.BossLock.SetActive(true);
        else
            Debug.LogError("Boss room lock is not assigned.", room);

        RobotBossAI boss = room.GetComponentInChildren<RobotBossAI>();
        if (boss != null)
            boss.Drop();
        else
            Debug.LogError("Boss room has no boss to drop.", room);
    }

    // Called by the shop to advance to the next level
    public static void AdvanceLevel()
    {
        if (currentLevelState == LevelState.Shop1)
            currentLevelState = LevelState.Stage2;
        else if (currentLevelState == LevelState.Stage2)
            currentLevelState = LevelState.Shop2;

        GameMenusController menus = FindAnyObjectByType<GameMenusController>();
        if (menus != null)
        {
            menus.Resume();
        }

        Time.timeScale = 1f;

        if (currentLevelState == LevelState.Stage2)
        {
            if (dungeonGenerator != null)
            {
                dungeonGenerator.Reset();
                dungeonGenerator.GenerateDungeon(currentLevelState == LevelState.Stage2);
                dungeonGenerator.MovePlayerToStart();
            }
        }
        else if (currentLevelState == LevelState.Shop2)
        {
            menus.Resume();
            menus.ShowWinMenu();
        }
    }

    public static void AdvanceToShop()
    {
        if (currentLevelState == LevelState.Stage1)
            currentLevelState = LevelState.Shop1;
        else if (currentLevelState == LevelState.Stage2)
            currentLevelState = LevelState.Shop2;

        GameMenusController menus = FindAnyObjectByType<GameMenusController>();
        if (menus != null)
        {
            menus.ShowShopMenu();
        }
    }
}
