using UnityEngine;

public class Coin : MonoBehaviour {
    private Transform Follow;
    private CharacterController Controller;
    private Vector3 MovementDirection;

    private int Value;

    private void Start() {
        this.transform.eulerAngles = new(0, Random.Range(0, 360), 0);
        this.Follow = GameObject.FindGameObjectWithTag("Player").transform;
        this.Controller = this.GetComponent<CharacterController>();
    }

    private void Update() {
        this.MovementDirection = this.Follow.position - this.transform.position;
    }

    private void FixedUpdate() {
        this.Controller.Move((PauseManager.GlobalSpeed / 10f) * this.MovementDirection.normalized);
    }

    public void Destroy() {
        LeanTween.scale(this.gameObject, new(0, 0, 0), 0.2f)
            .setDestroyOnComplete(true);
    }
}
