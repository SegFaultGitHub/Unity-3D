using UnityEngine;

public class CameraController : MonoBehaviour {
    private Transform Target;
    [SerializeField] private Vector3 Offset;

    private void Start() {
        this.Target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update() {
        this.transform.position = this.Target.position + this.Offset;
    }
}
