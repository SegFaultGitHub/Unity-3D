using UnityEngine;

public class Player : MonoBehaviour {
    private PlayerController PlayerController;

    public void Awake() {
        this.PlayerController = this.GetComponent<PlayerController>();
    }

    public void SetPosition(Vector3 position) {
        this.PlayerController.SetPosition(position);
    }
}
