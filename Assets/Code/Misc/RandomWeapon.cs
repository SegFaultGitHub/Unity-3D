using UnityEngine;

public class RandomWeapon : MonoBehaviour {
    public void Start() {
        int index = Random.Range(0, this.transform.childCount);
        for (int i = 0; i < this.transform.childCount; i++)
            this.transform.GetChild(i).gameObject.SetActive(i == index);
    }
}
