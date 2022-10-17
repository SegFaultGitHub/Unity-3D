using UnityEngine;

public class WeaponRack : MonoBehaviour {
    private void Start() {
        Transform leftWeapon = this.transform.Find("left-weapon");
        if (Utils.Rate(0.67f))
            leftWeapon.GetChild(Random.Range(0, leftWeapon.childCount)).gameObject.SetActive(true);
        Transform rightWeapon = this.transform.Find("right-weapon");
        if (Utils.Rate(0.67f))
            rightWeapon.GetChild(Random.Range(0, rightWeapon.childCount)).gameObject.SetActive(true);
    }
}
