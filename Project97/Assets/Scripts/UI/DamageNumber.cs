using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float floatSpeed = 40f;
    [SerializeField] private float lifetime = 1f;

    public void Setup(int damageAmount, bool heal = false)
    {
        if(!heal){
            damageText.text = damageAmount.ToString();
        }
        else
        {
            Debug.Log("heal damage popup");
            damageText.text = $"+{damageAmount}";
            damageText.color = AssetsDatabase.I.greenColour;

        }
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
    }
}
