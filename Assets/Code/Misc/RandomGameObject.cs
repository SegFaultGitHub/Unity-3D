using System.Collections.Generic;
using UnityEngine;

public class RandomGameObject : MonoBehaviour {
    [SerializeField] private List<RandomGameObjectWeight> Objects;
    [Header("X")]
    [SerializeField] private bool RandomRotateX;
    [SerializeField] private int AngleStepX = 1;
    [Header("Y")]
    [SerializeField] private bool RandomRotateY;
    [SerializeField] private int AngleStepY = 1;
    [Header("Z")]
    [SerializeField] private bool RandomRotateZ;
    [SerializeField] private int AngleStepZ = 1;

    private void Start() {
        if (this.Objects.Count == 0)
            Debug.LogError("Objects is empty!");

        foreach (RandomGameObjectWeight randomGameObject in this.Objects)
            randomGameObject.GameObject.SetActive(false);
        GameObject floor = Utils.RandomRange(this.Objects);
        floor.SetActive(true);
        Vector3 angles = floor.transform.eulerAngles;
        float x = angles.x, y = angles.y, z = angles.z;
        if (this.RandomRotateX) {
            x += Random.Range(0, 360 / this.AngleStepX) * this.AngleStepX;
        }
        if (this.RandomRotateY) {
            y += Random.Range(0, 360 / this.AngleStepY) * this.AngleStepY;
        }
        if (this.RandomRotateZ) {
            z += Random.Range(0, 360 / this.AngleStepZ) * this.AngleStepZ;
        }
        floor.transform.eulerAngles = new(x, y, z);
    }
}
