using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerController : MonoBehaviour {
    private CharacterController Controller;
    private Animator Animator;
    private float AnimatorSpeed;
    private Transform Camera;
    CinemachineFreeLook Cinemachine;

    private Vector3 MovementDirection;
    private Vector3 FallingSpeed;
    private float TurnSmoothVelocity;
    private readonly float TurnSmoothTime = 0.1f;
    private readonly Vector3 Gravity = new(0, -0.02f, 0);

    [SerializeField] private float MaxTargetDistance = 10;
    private float XMaxSpeed;
    [SerializeField] private GameObject CurrentTarget;
    [SerializeField] private GameObject LeftTarget;
    [SerializeField] private GameObject RightTarget;
    private bool Targetting;

    private List<Action> Interactions;

    private void Awake() {
        this.Camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        this.Cinemachine = GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineFreeLook>();
        this.XMaxSpeed = this.Cinemachine.m_XAxis.m_MaxSpeed;
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

        if (!this.Targetting) {
            Vector3 direction = Quaternion.AngleAxis(this.Camera.transform.eulerAngles.y, Vector3.up) * Vector3.forward;
            Vector3 from = this.transform.position + new Vector3(0, 1, 0);
            RaycastHit[] hits = Physics.RaycastAll(from, direction, this.MaxTargetDistance, LayerMask.GetMask("Enemy"));
            if (hits.Length != 0) {
                if (this.CurrentTarget == null) {
                    this.CurrentTarget = hits[0].collider.gameObject;
                    this.CurrentTarget.GetComponent<Outlineable>().Enable();
                } else if (hits[0].collider.gameObject != this.CurrentTarget) {
                    this.CurrentTarget.GetComponent<Outlineable>().Disable();
                    this.CurrentTarget = hits[0].collider.gameObject;
                    this.CurrentTarget.GetComponent<Outlineable>().Enable();
                }
            } else {
                if (this.CurrentTarget != null)
                    this.CurrentTarget.GetComponent<Outlineable>().Disable();
                this.CurrentTarget = null;
            }
            Debug.DrawLine(from, from + direction * this.MaxTargetDistance);
        } else {
            Vector3 diff = this.CurrentTarget.transform.position - this.transform.position;
            if (diff.magnitude > this.MaxTargetDistance) {
                this.ToggleTarget();
            } else {
                float targetAngle = Quaternion.FromToRotation(Vector3.forward, diff).eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(
                        this.Cinemachine.m_XAxis.Value,
                        targetAngle,
                        ref this.TurnSmoothVelocity, this.TurnSmoothTime / PauseManager.GlobalSpeed
                    );
                this.Cinemachine.m_XAxis.Value = angle;
            }

            bool targetFound;

            targetFound = false;
            for (int i = 0; i <= 60; i += 5) {
                Vector3 direction = Quaternion.AngleAxis(this.Camera.transform.eulerAngles.y + i, Vector3.up) * Vector3.forward;
                Vector3 from = this.transform.position + new Vector3(0, 1, 0);
                RaycastHit[] hits = Physics.RaycastAll(from, direction, this.MaxTargetDistance, LayerMask.GetMask("Enemy"));
                if (hits.Length == 0)
                    continue;

                Collider collider = hits[0].collider;
                if (collider.gameObject == this.CurrentTarget) {
                    if (hits.Length == 1)
                        continue;
                    collider = hits[1].collider;
                }
                if (collider.gameObject == this.CurrentTarget)
                    continue;

                this.RightTarget = collider.gameObject;
                targetFound = true;
                break;
            }
            if (!targetFound)
                this.RightTarget = null;

            targetFound = false;
            for (int i = 0; i >= -60; i -= 5) {
                Vector3 direction = Quaternion.AngleAxis(this.Camera.transform.eulerAngles.y + i, Vector3.up) * Vector3.forward;
                Vector3 from = this.transform.position + new Vector3(0, 1, 0);
                RaycastHit[] hits = Physics.RaycastAll(from, direction, this.MaxTargetDistance, LayerMask.GetMask("Enemy"));
                if (hits.Length == 0)
                    continue;

                Collider collider = hits[0].collider;
                if (collider.gameObject == this.CurrentTarget) {
                    if (hits.Length == 1)
                        continue;
                    collider = hits[1].collider;
                }
                if (collider.gameObject == this.CurrentTarget)
                    continue;

                this.LeftTarget = collider.gameObject;
                targetFound = true;
                break;
            }
            if (!targetFound)
                this.LeftTarget = null;
        }
    }

    private void FixedUpdate() {
        #region Movement
        if (!PauseManager.Paused) {
            if (this.MovementDirection.sqrMagnitude != 0) {
                float targetAngle = Mathf.Atan2(this.MovementDirection.x, this.MovementDirection.z) * Mathf.Rad2Deg + this.Camera.eulerAngles.y;
                if (this.Targetting) {
                    float angle = Mathf.SmoothDampAngle(
                        this.transform.eulerAngles.y,
                        Quaternion.FromToRotation(Vector3.forward, this.CurrentTarget.transform.position - this.transform.position).eulerAngles.y,
                        ref this.TurnSmoothVelocity, (this.TurnSmoothTime / 2) / PauseManager.GlobalSpeed
                    );
                    this.transform.rotation = Quaternion.Euler(0, angle, 0);
                } else {
                    float angle = Mathf.SmoothDampAngle(this.transform.eulerAngles.y, targetAngle, ref this.TurnSmoothVelocity, this.TurnSmoothTime / PauseManager.GlobalSpeed);
                    this.transform.rotation = Quaternion.Euler(0, angle, 0);
                }

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
        // --
        public bool TargetPressed;
        public bool SwitchTargetLeft;
        public bool SwitchTargetRight;
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
            // --
            TargetPressed = this.PlayerInputs.Controls.Target.WasPressedThisFrame(),
            SwitchTargetLeft = this.PlayerInputs.Controls.SwitchTargetLeft.WasPressedThisFrame(),
            SwitchTargetRight = this.PlayerInputs.Controls.SwitchTargetRight.WasPressedThisFrame(),
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

        if (this.Input.TargetPressed) {
            this.ToggleTarget();
        }
        if (this.Input.SwitchTargetLeft) {
            this.SwitchTargetLeft();
        } else if (this.Input.SwitchTargetRight) {
            this.SwitchTargetRight();
        }

        //Debug
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
        if (this.Targetting)
            return;
        if (this.Interactions.Count == 0)
            return;

        foreach (Action action in this.Interactions) {
            action.Invoke();
        }
    }

    private void ToggleTarget() {
        if (this.Targetting) {
            this.Targetting = false;
            this.Cinemachine.m_XAxis.m_MaxSpeed = this.XMaxSpeed;
            this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.white);
        } else {
            if (this.CurrentTarget == null)
                return;
            this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.red);
            this.Targetting = true;
            this.Cinemachine.m_XAxis.m_MaxSpeed = 0;
        }
    }

    private void SwitchTargetLeft() {
        if (!this.Targetting || this.LeftTarget == null)
            return;
        this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.white);
        this.CurrentTarget.GetComponent<Outlineable>().Disable();
        this.CurrentTarget = this.LeftTarget;
        this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.red);
        this.CurrentTarget.GetComponent<Outlineable>().Enable();
    }

    private void SwitchTargetRight() {
        if (!this.Targetting || this.RightTarget == null)
            return;
        this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.white);
        this.CurrentTarget.GetComponent<Outlineable>().Disable();
        this.CurrentTarget = this.RightTarget;
        this.CurrentTarget.GetComponent<Outlineable>().ChangeColor(Color.red);
        this.CurrentTarget.GetComponent<Outlineable>().Enable();
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
        } else if (collider.CompareTag("Door/Right")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Right);
        } else if (collider.CompareTag("Door/Up")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Up);
        } else if (collider.CompareTag("Door/Down")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Down);
        }

        if (collider.CompareTag("Coin")) {
            Coin coin = collider.GetComponentInParent<Coin>();
            coin.Destroy();
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
