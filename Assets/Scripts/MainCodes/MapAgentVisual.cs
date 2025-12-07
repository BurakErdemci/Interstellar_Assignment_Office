using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapAgentVisual : MonoBehaviour
{
    [Header("Components")]
    public Image agentIcon; 
    public float moveSpeed = 300f; 

    [Header("Animation Settings")]
    public float rotationOffset = 0f;
    public float waddleSpeed = 15f;     
    public float waddleAmount = 10f;    

    public void MoveTo(Vector3 targetPos, System.Action onArrival)
    {
        StartCoroutine(MoveRoutine(targetPos, onArrival));
    }

 
    // isReturning: True ise "Dönüş", False ise "Gidiş"
    public void Setup(AgentData data, bool isReturning)
    {
        if(data.faceIcon != null) 
            agentIcon.sprite = data.faceIcon;

        string quote = "";

        if (isReturning)
        {
            // DÖNÜYORSA: Dönüş listesinden rastgele seç
            if (data.returnQuotes.Length > 0)
                quote = data.GetRandomQuote(data.returnQuotes);
        }
        else
        {
            // GİDİYORSA: Gidiş listesinden rastgele seç
            if (data.travelQuotes.Length > 0)
                quote = data.GetRandomQuote(data.travelQuotes);
        }

        // Eğer bir söz seçildiyse göster
        if (!string.IsNullOrEmpty(quote))
        {
            StartCoroutine(TriggerNotification(quote, data.faceIcon));
        }
    }

    IEnumerator TriggerNotification(string message, Sprite icon)
    {
        yield return new WaitForSeconds(0.5f);
        NotificationManager.Instance.ShowMessage(message, icon);
    }

    IEnumerator MoveRoutine(Vector3 target, System.Action onArrival)
    {
     
        while (Vector3.Distance(transform.localPosition, target) > 5f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, moveSpeed * Time.deltaTime);

            Vector3 direction = (target - transform.localPosition).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (Mathf.Abs(targetAngle) > 90) transform.localScale = new Vector3(1, -1, 1);
            else transform.localScale = new Vector3(1, 1, 1);

            float waddle = Mathf.Sin(Time.time * waddleSpeed) * waddleAmount;
            transform.localRotation = Quaternion.Euler(0, 0, targetAngle + rotationOffset + waddle);

            yield return null;
        }

        transform.localPosition = target;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one; 
        
        onArrival?.Invoke();
    }
}