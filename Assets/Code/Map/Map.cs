using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class Map : MonoBehaviour {
    #region Data
    private class RoomEntry {
        public Direction OriginalDirection;
        public Vector2Int Position;
        public Dictionary<Direction, RoomEntry> Doors;
        public List<DirectionGroup> DirectionGroups;
        public bool Start, End;
        public bool Hub;
        public Room Room;

        public RoomEntry(Vector2Int position) {
            this.Position = position;
            this.Doors = new();
        }

        public void UpdateDirectionGroups() {
            this.DirectionGroups = new() { new() { Directions = this.Doors.Keys.ToList() } };
        }

        public Dictionary<Direction, RoomEntry> AccessibleDoors(Direction from) {
            List<Direction> directions = this.DirectionGroups.Find(directionGroup => directionGroup.Directions.Contains(OppositeDirection(from))).Directions;
            if (directions == null) {
                return new();
            }
            Dictionary<Direction, RoomEntry> roomEntries = new();
            directions.ForEach(direction => {
                roomEntries[direction] = this.Doors[direction];
            });
            return roomEntries;
        }
    }

    [Serializable]
    public struct DirectionGroup {
        public List<Direction> Directions;
    }

    private struct MapGraphEntry {
        public Direction From;
        public Vector2Int Position;

        public override string ToString() {
            return this.Position + "/" + this.From;
        }
    }

    private struct CreatedRoom {
        public Vector2Int Position;
        public bool Created;
    }
    private struct AdjacentRoom {
        public Vector2Int Position;
        public Direction Door;
    }
    [Serializable]
    public struct _MapLayoutData {
        public int MinI, MinJ, MaxI, MaxJ;
        public Vector2Int RoomSize;
        public Vector2Int DoorOffset;
        public Vector2Int RoomOffset;
        public Vector2Int TextureSize;
        public Texture2D Texture;
    }
    #endregion

    [Header("Map attributes")]
    [Tooltip("The minimum number of doors in the first room")]
    [Range(1, 4)]
    [SerializeField] private int MinDirections = 1;
    [Tooltip("The maximum number of doors in the first room")]
    [Range(1, 4)]
    [SerializeField] private int MaxDirections = 4;
    [Tooltip("The minimum length of each path from the start")]
    [Range(1, 15)]
    [SerializeField] private int MinLength = 1;
    [Tooltip("The minimum length of each path from the start")]
    [Range(1, 15)]
    [SerializeField] private int MaxLength = 4;
    [SerializeField][Range(1, 3)] private float AlternatePathDecreaseRatio = 1.2f;
    [Tooltip("Check if you want the paths to be longer if there is fewer paths")]
    [SerializeField] private bool IncreasePathLength = true;
    [SerializeField] private int Seed;

    [SerializeField] private List<Room> RoomTemplates;

    private Dictionary<Vector2Int, RoomEntry> RoomEntries;
    private Vector2Int CurrentRoomPosition = Vector2Int.zero;

    public Room CurrentRoom { get => this.RoomEntries[this.CurrentRoomPosition].Room; }
    public _MapLayoutData MapLayoutData;

    private Player Player;
    private FadeScreen FadeScreen;

    private void Awake() {
        this.RoomTemplates = Resources.LoadAll<Room>("Rooms").Where(room => room.Enabled).ToList();

        this.GenerateMap();
        this.GenerateHubs();
        this.SetRooms();
        this.GenerateSpecialRooms();
    }

    private void Start() {
        this.Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        this.FadeScreen = GameObject.FindGameObjectWithTag("FadeScreen").GetComponent<FadeScreen>();

        this.ChangeRoomAndSetPosition(Vector2Int.zero);
    }

   private void ChangeRoomAndSetPosition(Vector2Int position) {
        this.ChangeRoom(position);
        this.Player.SetPosition(this.CurrentRoom.StartingPosition.position);

    }

    private void ChangeRoom(Vector2Int position) {
        this.CurrentRoom.gameObject.SetActive(false);
        this.CurrentRoomPosition = position;
        this.CurrentRoom.gameObject.SetActive(true);
    }

    public void ChangeRoom(Direction direction) {
        PauseManager.Pause();
        this.FadeScreen.Fade(0.3f)
            .setOnComplete(() => {
                switch (direction) {
                    case Direction.Left:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x - 1, this.CurrentRoomPosition.y));
                        break;
                    case Direction.Right:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x + 1, this.CurrentRoomPosition.y));
                        break;
                    case Direction.Up:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x, this.CurrentRoomPosition.y - 1));
                        break;
                    case Direction.Down:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x, this.CurrentRoomPosition.y + 1));
                        break;
                };
                this.CurrentRoom.Transitions[OppositeDirection(direction)].Transform.GetComponentInChildren<Door>().OpenSide1();
                this.Player.SetPosition(this.CurrentRoom.Transitions[OppositeDirection(direction)].Position);

                this.FadeScreen.Unfade(0.3f)
                    .setDelay(0.5f)
                    .setOnComplete(() => PauseManager.Unpause());
            });
    }

    private void GenerateMap() {
        if (this.Seed != 0) {
            Random.InitState(this.Seed);
        } else {
            this.Seed = Random.seed;
        }

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        int count = Random.Range(this.MinDirections, this.MaxDirections + 1);
        float ratio = this.IncreasePathLength ? 4f / count : 1;
        directions = Utils.Sample(directions, count);

        this.RoomEntries = new() {
            [new(0, 0)] = new RoomEntry(new(0, 0)) { Hub = false, OriginalDirection = Direction.None }
        };
        this.RoomEntries[new(0, 0)].Start = true;

        directions.ForEach(direction => {
            CreatedRoom room = this.LinkRooms(direction, new(0, 0), direction);
            float pathLength = Random.Range(this.MinLength, this.MaxLength + 1) * ratio * 2;
            float alternatePathRate = 1f / Mathf.Pow(pathLength, this.AlternatePathDecreaseRatio);

            this.GeneratePath(direction, room.Position, direction, direction, pathLength, alternatePathRate);
        });
    }

    private void SetRooms() {
        this.RoomEntries.Values.ToList().ForEach(roomEntry => {
            List<Room> rooms = this.RoomTemplates.Where(room => this.IsRoomValid(room, roomEntry.Doors.Keys.ToList())).ToList();
            Room room = null;
            if (roomEntry.Hub) {
                List<Room> hubs = rooms.Where(room => {
                    return room.Hub && this.DirectionGroupsMatch(room, roomEntry);
                }).ToList();
                if (hubs.Count > 0) {
                    room = Utils.Sample(hubs);
                }
            } else {
                room = this.SelectRoom(rooms, roomEntry);
            }
            if (room == null) {
                room = this.SelectRoom(rooms, roomEntry);
            }
            roomEntry.Room = this.SetRoom(room, roomEntry.Doors.Keys.ToList(), roomEntry.Position);
            roomEntry.Room.Hub = roomEntry.Hub;
            roomEntry.Room.OriginalDirection = roomEntry.OriginalDirection;
            if (this.MapLayoutData.MinI > roomEntry.Room.Position.x) { this.MapLayoutData.MinI = roomEntry.Room.Position.x; }
            if (this.MapLayoutData.MaxI < roomEntry.Room.Position.x) { this.MapLayoutData.MaxI = roomEntry.Room.Position.x; }
            if (this.MapLayoutData.MinJ > roomEntry.Room.Position.y) { this.MapLayoutData.MinJ = roomEntry.Room.Position.y; }
            if (this.MapLayoutData.MaxJ < roomEntry.Room.Position.y) { this.MapLayoutData.MaxJ = roomEntry.Room.Position.y; }
        });
    }

    private Room SelectRoom(List<Room> rooms, RoomEntry roomEntry) {
        List<string> adjacentRoomNames = roomEntry.Doors.Values.Select(roomEntry => roomEntry.Room == null ? "" : roomEntry.Room.PrefabName).ToList();
        List<Room> possibleRooms = rooms.Where(room => !room.Hub && !adjacentRoomNames.Contains(room.name)).ToList();
        if (possibleRooms.Count == 0)
            return Utils.Sample(rooms.Where(room => !room.Hub).ToList());
        else
            return Utils.Sample(possibleRooms);
    }

    private bool DirectionGroupsMatch(Room room, RoomEntry roomEntry) {
        return roomEntry.DirectionGroups.All(roomEntryDirectionGroup => {
            if (roomEntryDirectionGroup.Directions.Count == 0) { return false; }
            DirectionGroup roomDirectionGroup = room.DirectionGroups.Find(roomDirectionGroup => roomDirectionGroup.Directions.Contains(roomEntryDirectionGroup.Directions[0]));

            return roomEntryDirectionGroup.Directions.All(direction => roomDirectionGroup.Directions.Contains(direction));
        });
    }

    private void GenerateHubs() {
        this.RoomEntries.Values.ToList().ForEach(roomEntry => roomEntry.UpdateDirectionGroups());

        List<List<DirectionGroup>> allPossibleHubDirectionGroups = this.RoomTemplates.Where(room => room.Hub).Select(room => room.DirectionGroups).ToList();

        this.RoomEntries.Values.ToList().Where(roomEntry => roomEntry.Hub).ToList().ForEach(roomEntry => {
            List<List<DirectionGroup>> currentPossibleDirectionGroups = Utils
                .Shuffle(allPossibleHubDirectionGroups)
                .Select(directionGroups => {
                    return directionGroups.Select(directionGroup => {
                        return new DirectionGroup() { Directions = directionGroup.Directions.Where(direction => roomEntry.Doors.Keys.Contains(direction)).ToList() };
                    }).ToList();
                }).ToList();

            foreach (List<DirectionGroup> directionGroups in currentPossibleDirectionGroups) {
                roomEntry.DirectionGroups = directionGroups;
                if (this.CheckPaths()) {
                    return;
                }
            }
            roomEntry.DirectionGroups = new() { new() { Directions = roomEntry.Doors.Keys.ToList() } };
            roomEntry.Hub = false;
        });
    }

    private void GenerateSpecialRooms() {
        List<Room> specialRooms = Utils.Shuffle(
            this.RoomEntries.Values
                .Where(roomEntry => roomEntry.End).ToList()
                .Select(roomEntry => roomEntry.Room).ToList()
        );
        specialRooms[0].GenerateBoss();
        float x = specialRooms.Count;
        float rate = x / (3 * (x - 1) + 1);
        specialRooms.Skip(1).ToList().ForEach(room => {
            if (Utils.Rate(rate)) { room.GenerateTreasure(); }
        });
    }

    private void GeneratePath(Direction originalDirection, Vector2Int position, Direction previousDirection, Direction mainDirection, float pathLength, float alternatePathRate) {
        if (pathLength <= 0) {
            this.RoomEntries[position].End = true;
            return;
        }

        this.RoomEntries[position].End = false;

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        directions.RemoveAll(_direction => _direction == OppositeDirection(previousDirection) || _direction == OppositeDirection(mainDirection));
        Direction nextDirection = Utils.Sample(directions);

        CreatedRoom room = this.LinkRooms(originalDirection, position, nextDirection);
        if (!room.Created && pathLength <= 1) { pathLength++; }
        this.RoomEntries[room.Position].End = false;
        this.GeneratePath(originalDirection, room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate);

        if (Utils.Rate(alternatePathRate)) {
            directions.Remove(nextDirection);
            nextDirection = Utils.Sample(directions);
            room = this.LinkRooms(originalDirection, position, nextDirection);
            if (!room.Created && pathLength <= 1) { pathLength++; }
            this.RoomEntries[room.Position].End = false;
            this.GeneratePath(originalDirection, room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate * 0.75f);
        }
    }

    private static Direction OppositeDirection(Direction direction) {
        return direction switch {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new("[Map:OppositeDirection] Unexpection direction " + direction + "."),
        };
    }

    private CreatedRoom LinkRooms(Direction originalDirection, Vector2Int position, Direction direction) {
        int x = position.x;
        int y = position.y;
        var adjacentRoom = direction switch {
            Direction.Up => new AdjacentRoom { Position = new(x, y - 1) },
            Direction.Down => new AdjacentRoom { Position = new(x, y + 1) },
            Direction.Left => new AdjacentRoom { Position = new(x - 1, y) },
            Direction.Right => new AdjacentRoom { Position = new(x + 1, y) },
            _ => throw new("[Map:LinkRooms] Unexpection direction " + direction + "."),
        };
        if (!this.RoomEntries.ContainsKey(adjacentRoom.Position)) {
            // Room empty
            // Setting new room
            this.RoomEntries[adjacentRoom.Position] = new RoomEntry(adjacentRoom.Position) { OriginalDirection = originalDirection, Hub = false };

            // Setting bidirectional doorway
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[position].Doors[direction] = this.RoomEntries[adjacentRoom.Position];

            return new CreatedRoom { Position = adjacentRoom.Position, Created = true };
        } else {
            // Room already existing, setting doorway between the rooms
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[adjacentRoom.Position].Hub = true;
            this.RoomEntries[position].Doors[direction] = this.RoomEntries[adjacentRoom.Position];

            return new CreatedRoom { Position = adjacentRoom.Position, Created = false };
        }
    }

    private bool IsRoomValid(Room room, List<Direction> directions) {
        return directions.All(direction => {
            return direction switch {
                Direction.Up => room.UpDoorAllowed,
                Direction.Right => room.RightDoorAllowed,
                Direction.Down => room.DownDoorAllowed,
                Direction.Left => room.LeftDoorAllowed,
                _ => throw new("[Map:IsRoomValid] Unexpection direction " + direction + "."),
            };
        });
    }

    private Room SetRoom(Room roomPrefab, List<Direction> directions, Vector2Int position) {
        Room room = Instantiate(roomPrefab, this.transform);
        foreach (Direction direction in (new Direction[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })) {
            room.SetDirectionActive(direction, directions.Contains(direction));
        }
        room.Position = position;
        room.gameObject.SetActive(false);
        room.name = "Room: " + position.x + "/" + position.y + " [" + roomPrefab.name + "]";
        room.PrefabName = roomPrefab.name;

        return room;
    }

    private bool CheckPaths() {
        Dictionary<Vector2Int, bool> visitedRooms = new() {
            [Vector2Int.zero] = true,
        };
        Dictionary<MapGraphEntry, bool> foo = new() {
            [new() { From = Direction.None, Position = Vector2Int.zero }] = true,
        };

        Dictionary<Direction, RoomEntry> doors = this.RoomEntries[new()].Doors;
        foreach (KeyValuePair<Direction, RoomEntry> keyValue in doors) {
            this._CheckPath(keyValue.Value.Position, keyValue.Key, visitedRooms, foo);
        }

        return visitedRooms.Count == this.RoomEntries.Count;
    }

    private void _CheckPath(Vector2Int position, Direction from, Dictionary<Vector2Int, bool> visitedRooms, Dictionary<MapGraphEntry, bool> foo) {
        Dictionary<Direction, RoomEntry> keyValues = this.RoomEntries[position].AccessibleDoors(from);
        foreach (KeyValuePair<Direction, RoomEntry> keyValue in keyValues) {
            MapGraphEntry mapGraphEntry = new() {
                Position = keyValue.Value.Position,
                From = from,
            };

            if (foo.ContainsKey(mapGraphEntry)) { continue; }

            foo[mapGraphEntry] = true;
            visitedRooms[keyValue.Value.Position] = true;

            this._CheckPath(keyValue.Value.Position, keyValue.Key, visitedRooms, foo);
        }
    }
}
