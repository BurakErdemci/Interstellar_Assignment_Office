using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapAgentVisual : MonoBehaviour
{
    public Image agentIcon; 
    public float moveSpeed = 300f; 

    [Header("Animation Settings")]
    public float rotationOffset = 0f;   // Bunu 0 yaparsan muhtemelen düzelir artık
    public float waddleSpeed = 15f;     
    public float waddleAmount = 10f;    

    public void MoveTo(Vector3 targetPos, System.Action onArrival)
    {
        StartCoroutine(MoveRoutine(targetPos, onArrival));
    }

    public void Setup(AgentData data)
    {
        if(data.faceIcon != null)
        {
            agentIcon.sprite = data.faceIcon;
        }
    }

    IEnumerator MoveRoutine(Vector3 target, System.Action onArrival)
    {
        while (Vector3.Distance(transform.localPosition, target) > 5f)
        {
            // 1. HAREKET
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, moveSpeed * Time.deltaTime);

            // 2. YÖN VE AÇI HESAPLAMA
            Vector3 direction = (target - transform.localPosition).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // --- YENİ EKLENEN KISIM: AYNA (FLIP) MANTIĞI ---
            // Eğer açımız 90'dan büyükse veya -90'dan küçükse (Yani Sola/Geriye gidiyorsa)
            // Resmi Y ekseninde ters çevir ki kafa aşağı gelmesin.
            if (Mathf.Abs(targetAngle) > 90)
            {
                transform.localScale = new Vector3(1, -1, 1); // Ters çevir (Ayna)
            }
            else
            {
                transform.localScale = new Vector3(1, 1, 1); // Düzelt
            }
            // -----------------------------------------------

            // 3. SALLANMA
            float waddle = Mathf.Sin(Time.time * waddleSpeed) * waddleAmount;

            // 4. DÖNDÜRME
            transform.localRotation = Quaternion.Euler(0, 0, targetAngle + rotationOffset + waddle);

            yield return null;
        }

        transform.localPosition = target;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one; // Varınca boyutu düzelt
        
        onArrival?.Invoke();
    }
}