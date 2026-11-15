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
    }

    // Update is called once per frame
    void Update()
    {
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
