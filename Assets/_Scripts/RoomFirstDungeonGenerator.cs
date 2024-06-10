using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    [SerializeField]
    private int minRoomWidth = 4, minRoomHeight = 4;
    [SerializeField]
    private int dungeonWidth = 20, dungeonHeight = 20;
    [SerializeField]
    [Range(0, 10)]
    private int offset = 1;
    [SerializeField]
    private bool randomWalkRooms = false;
    List<BoundsInt> roomsList;

    [SerializeField]
    private GameObject objectPrefab; // Prefab for the objects (pieces)
    [SerializeField]
    private GameObject enemyPrefab; // Prefab for enemies
    [SerializeField]
    private int enemyCountPerRoom = 1; // Number of enemies per room

    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private List<GameObject> instantiatedEnemies = new List<GameObject>();

    private void Start()
    {
        tilemapVisualizer.Clear();
        RunProceduralGeneration();
    }
    protected override void RunProceduralGeneration()
    {
        DestroyOldObjectsAndEnemies();
        CreateRooms();
        // Place objects (pieces) and enemies in rooms
        PlaceObjectsAndEnemies();
        //InvokeRepeating("move_enemy", 5.0f, 2.0f);
    }

     /*private void move_enemy()
     {
         foreach (var enemy in instantiatedEnemies)
         {
             Vector2 currentPos = enemy.transform.position;
             // Ajoute la première direction de la liste
             Vector2 newPos = currentPos + (Vector2)Direction2D.cardinalDirectionsList[0];
             // Met à jour la position de l'ennemi
             enemy.transform.position = new Vector2(newPos.x, newPos.y);
         }

     }
     */
    private void CreateRooms()
    {
        roomsList = new List<BoundsInt>();
        roomsList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        if (randomWalkRooms)
        {
            floor = CreateRoomsRandomly(roomsList);
        }
        else
        {
            floor = CreateSimpleRooms(roomsList);
        }


        List<Vector2Int> roomCenters = new List<Vector2Int>();
        foreach (var room in roomsList)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }

        HashSet<Vector2Int> corridors = ConnectRooms(roomCenters);
        floor.UnionWith(corridors);

        tilemapVisualizer.PaintFloorTiles(floor);
        WallGenerator.CreateWalls(floor, tilemapVisualizer);


    }
    private void PlaceObjectsAndEnemies()
    {
        foreach (var room in roomsList)
        {
            // Place objects (pieces)
            Vector3Int objectPosition = new Vector3Int(
                Random.Range(room.xMin + offset, room.xMax - offset),
                Random.Range(room.yMin + offset, room.yMax - offset),
                0);
            GameObject instantiatedObject = Instantiate(objectPrefab, (Vector3)objectPosition, Quaternion.identity);
            instantiatedObjects.Add(instantiatedObject);

            // Place enemies
            for (int i = 0; i < enemyCountPerRoom; i++)
            {
                Vector3Int enemyPosition = new Vector3Int(
                    Random.Range(room.xMin + offset, room.xMax - offset),
                    Random.Range(room.yMin + offset, room.yMax - offset),
                    0);
                GameObject instantiatedEnemy = Instantiate(enemyPrefab, (Vector3)enemyPosition, Quaternion.identity);
                instantiatedEnemies.Add(instantiatedEnemy);
            }
        }
    }

    private void DestroyOldObjectsAndEnemies()
    {
        foreach (var obj in instantiatedObjects)
        {
            Destroy(obj);
        }
        //instantiatedObjects.Clear();

        foreach (var enemy in instantiatedEnemies)
        {
            Destroy(enemy);
        }
        //instantiatedEnemies.Clear();
    }
    private HashSet<Vector2Int> CreateRoomsRandomly(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        for (int i = 0; i < roomsList.Count; i++)
        {
            var roomBounds = roomsList[i];
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBounds.center.x), Mathf.RoundToInt(roomBounds.center.y));
            var roomFloor = RunRandomWalk(randomWalkParameters, roomCenter);
            foreach (var position in roomFloor)
            {
                if (position.x >= (roomBounds.xMin + offset) && position.x <= (roomBounds.xMax - offset) && position.y >= (roomBounds.yMin - offset) && position.y <= (roomBounds.yMax - offset))
                {
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomCenters)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);
        while (position.y != destination.y)
        {
            if (destination.y > position.y)
            {
                position += Vector2Int.up;
            }
            else if (destination.y < position.y)
            {
                position += Vector2Int.down;
            }
            corridor.Add(position);
        }
        while (position.x != destination.x)
        {
            if (destination.x > position.x)
            {
                position += Vector2Int.right;
            }
            else if (destination.x < position.x)
            {
                position += Vector2Int.left;
            }
            corridor.Add(position);
        }
        return corridor;
    }

    private Vector2Int FindClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;
        foreach (var position in roomCenters)
        {
            float currentDistance = Vector2.Distance(position, currentRoomCenter);
            if (currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }
        return closest;
    }

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomsList)
        {
            for (int col = offset; col < room.size.x - offset; col++)
            {
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }


}
