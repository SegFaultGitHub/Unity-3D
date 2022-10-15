using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour {
    public struct Transition {
        public Transform Transform;
        public Vector3 Position;
    }

    public Transform StartingPosition;
    public bool Enabled;

    [HideInInspector] public Vector2Int Position;
    [HideInInspector] public Dictionary<Direction, Transition> Transitions { get; private set; }

    [Header("Allowed Doors")]
    public bool LeftDoorAllowed = true;
    public bool RightDoorAllowed = true;
    public bool UpDoorAllowed = true;
    public bool DownDoorAllowed = true;

    [Header("Doorways")]
    [SerializeField] public List<Map.DirectionGroup> DirectionGroups;

    [Header("Special")]
    [SerializeField] private bool BossRoom;
    [SerializeField] private bool TreasureRoom;

    [Header("Debug data")]
    public bool Hub;
    public Direction OriginalDirection;

    private void Awake() {
        this.Transitions = new Dictionary<Direction, Transition> {
            [Direction.Up] = new Transition {
                Transform = this.transform.Find("default/transitions/up"),
                Position = this.transform.Find("default/transitions/up/transition position").position,
            },
            [Direction.Down] = new Transition {
                Transform = this.transform.Find("default/transitions/down"),
                Position = this.transform.Find("default/transitions/down/transition position").position,
            },
            [Direction.Left] = new Transition {
                Transform = this.transform.Find("default/transitions/left"),
                Position = this.transform.Find("default/transitions/left/transition position").position,
            },
            [Direction.Right] = new Transition {
                Transform = this.transform.Find("default/transitions/right"),
                Position = this.transform.Find("default/transitions/right/transition position").position,
            },
        };

        this.StartingPosition = this.transform.Find("default/starting position");
    }

    public void SetDirectionActive(Direction direction, bool active) {
        this.Transitions[direction].Transform.Find("active").gameObject.SetActive(active);
        this.Transitions[direction].Transform.Find("inactive").gameObject.SetActive(!active);
    }

    public void GenerateBoss() {
        this.BossRoom = true;
        this.gameObject.name += " (Boss)";
    }

    public void GenerateTreasure() {
        //this.transform.Find("Treasures").gameObject.SetActive(true);
        this.TreasureRoom = true;
        this.gameObject.name += " (Treasure)";
    }
}
