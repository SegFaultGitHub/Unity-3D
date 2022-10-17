using System;
using System.Collections;
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
        if (PauseManager.Paused)
            this.Animator.speed = 0;
        else 
            this.Animator.speed = PauseManager.GlobalSpeed * this.AnimatorSpeed;

        this.GatherInput();
        this.HandleInput();
    }

    private void FixedUpdate() {
        #region Movement
        if (!PauseManager.Paused) {
            if (this.MovementDirection.sqrMagnitude != 0) {
                float targetAngle = Mathf.Atan2(this.MovementDirection.x, this.MovementDirection.z) * Mathf.Rad2Deg + this.Camera.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(this.transform.eulerAngles.y, targetAngle, ref this.TurnSmoothVelocity, this.TurnSmoothTime / PauseManager.GlobalSpeed);
                this.transform.rotation = Quaternion.Euler(0, angle, 0);

                Vector3 movementDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
                this.Controller.Move((PauseManager.GlobalSpeed / 5f) * movementDirection.normalized);
            }

            CollisionFlags flags = this.Controller.Move(PauseManager.GlobalSpeed * this.FallingSpeed);
            if (flags == CollisionFlags.Below) {
                this.FallingSpeed = this.Gravity;
            } else {
                this.FallingSpeed += this.Gravity;
            }
        }
        #endregion
    }

    public void SetPosition(Vector3 position) {
        this.Controller.enabled = false;
        this.transform.position = position;
        this.Controller.enabled = true;
    }

    #region Input
    private PlayerInputs PlayerInputs;
    private class _Input {
        public bool MoveStarted;
        public bool MoveInProgress;
        public bool MoveEnded;
        // --
        public bool AttackPressed;
        // --
        public bool InteractPressed;
        //Debug
        public bool HitPressed;
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
            // --
            AttackPressed = false,
            // --
            InteractPressed = this.PlayerInputs.Controls.Interact.WasPressedThisFrame(),
            //Debug
            HitPressed = this.PlayerInputs.Controls.Hit.WasPressedThisFrame(),
        };
    }

    private void HandleInput() {
        if (this.Input.InteractPressed) {
            this.Interact();
        }

        if (this.Input.MoveStarted || this.Input.MoveInProgress || this.Input.MoveEnded) {
            this.Move(this.PlayerInputs.Controls.Move.ReadValue<Vector2>());
        }

        if (this.Input.HitPressed) {
            this.Hit();
        }
    }
    #endregion

    private void Move(Vector2 direction) {
        this.MovementDirection = new Vector3(direction.x, 0, direction.y).normalized;

        this.Animator.SetBool("Moving", this.MovementDirection.sqrMagnitude != 0);
    }

    private void Interact() {
        if (this.Interactions.Count == 0)
            return;

        foreach (Action action in this.Interactions) {
            action.Invoke();
        }
    }

    private void Hit() {
        PauseManager.SetGlobalSpeed(0.1f, 0f)
            .setOnComplete(() => this.StartCoroutine(this.ResetTimeScale()));
    }

    private IEnumerator ResetTimeScale() {
        yield return new WaitForSeconds(2f);
        PauseManager.SetGlobalSpeed(1, 0f);
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.CompareTag("Door/Side1")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Add(door.OpenSide1);
            if (!door.Opened)
                door.Enable();
        } else if (collider.CompareTag("Door/Side2")) {
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

        if (collider.CompareTag("Door/Left")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Left);
            return;
        } else if (collider.CompareTag("Door/Right")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Right);
            return;
        } else if (collider.CompareTag("Door/Up")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Up);
            return;
        } else if (collider.CompareTag("Door/Down")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Down);
            return;
        }
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.CompareTag("Door/Side1")) {
            Door door = collider.GetComponentInParent<Door>();
            this.Interactions.Remove(door.OpenSide1);
            door.Disable();
        } else if (collider.CompareTag("Door/Side2")) {
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
