using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerController : MonoBehaviour {
    private CharacterController Controller;
    private Animator Animator;
    private float AnimatorSpeed;
    private Transform Camera;

    private Vector3 MovementDirection;
    [SerializeField] private Vector3 FallingSpeed;
    private float TurnSmoothVelocity;
    private readonly float TurnSmoothTime = 0.1f;
    private readonly Vector3 Gravity = new(0, -0.02f, 0);

    private List<Action> Interactions;

    private void Awake() {
        this.Camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        this.Controller = this.GetComponent<CharacterController>();
        this.Animator = this.GetComponentInChildren<Animator>();
        this.AnimatorSpeed = this.Animator.speed;
        this.FallingSpeed = this.Gravity;
        this.Interactions = new();
    }

    private void Update() {
        this.Animator.speed = PauseManager.Paused ? 0 : this.AnimatorSpeed;

        this.GatherInput();
        this.HandleInput();
    }

    #region Input
    private PlayerInputs PlayerInputs;
    private class _Input {
        public bool MoveStarted;
        public bool MoveInProgress;
        public bool MoveEnded;

        public bool AttackPressed;

        public bool InteractPressed;
    }
    private _Input Input;

    private void OnEnable() {
        this.PlayerInputs = new PlayerInputs();
        this.PlayerInputs.Controls.Enable();
    }

    private void OnDisable() {
        this.PlayerInputs.Controls.Disable();
    }

    private void GatherInput() {
        this.Input = new() {
            MoveStarted = this.PlayerInputs.Controls.Move.WasPressedThisFrame(),
            MoveInProgress = this.PlayerInputs.Controls.Move.IsPressed(),
            MoveEnded = this.PlayerInputs.Controls.Move.WasReleasedThisFrame(),

            AttackPressed = false,

            InteractPressed = this.PlayerInputs.Controls.Interact.WasPressedThisFrame(),
        };
    }

    private void HandleInput() {
        if (this.Input.InteractPressed) {
            this.Interact();
        }

        if (this.Input.MoveStarted || this.Input.MoveInProgress || this.Input.MoveEnded) {
            this.Move(this.PlayerInputs.Controls.Move.ReadValue<Vector2>());
        }
    }
    #endregion

    private void Move(Vector2 direction) {
        this.MovementDirection = new Vector3(direction.x, 0, direction.y).normalized;

        this.Animator.SetBool("Moving", this.MovementDirection.sqrMagnitude != 0);
    }

    private void Interact() {
        foreach (Action action in this.Interactions) {
            action.Invoke();
        }
    }

    private void FixedUpdate() {
        #region Movement
        if (!PauseManager.Paused) {
            if (this.MovementDirection.sqrMagnitude != 0) {
                float targetAngle = Mathf.Atan2(this.MovementDirection.x, this.MovementDirection.z) * Mathf.Rad2Deg + this.Camera.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(this.transform.eulerAngles.y, targetAngle, ref this.TurnSmoothVelocity, this.TurnSmoothTime);
                this.transform.rotation = Quaternion.Euler(0, angle, 0);

                Vector3 movementDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
                this.Controller.Move(movementDirection.normalized / 10);
            }

            CollisionFlags flags = this.Controller.Move(this.FallingSpeed);
            if (flags == CollisionFlags.Below) {
                this.FallingSpeed = this.Gravity;
            } else {
                this.FallingSpeed += this.Gravity;
            }
        }
        #endregion
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.CompareTag("DoorSide1")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Add(door.OpenSide1);
            if (!door.Opened)
                door.Enable();
        } else if (collider.CompareTag("DoorSide2")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Add(door.OpenSide2);
            if (!door.Opened)
                door.Enable();
        } else if (collider.CompareTag("Chest")) {
            Chest chest = collider.GetComponentInParent<Chest>();
            this.Interactions.Add(chest.Open);
            if (!chest.Opened)
                chest.Enable();
        }
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.CompareTag("DoorSide1")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Remove(door.OpenSide1);
            door.Disable();
        } else if (collider.CompareTag("DoorSide2")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Remove(door.OpenSide2);
            door.Disable();
        } else if (collider.CompareTag("Chest")) {
            Chest chest = collider.GetComponentInParent<Chest>();
            this.Interactions.Remove(chest.Open);
            chest.Disable();
        }
    }
}
