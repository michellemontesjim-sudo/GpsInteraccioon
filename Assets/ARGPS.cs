using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using System.Collections;

// Esta clase agrupa los datos de cada objeto para que se vean organizados en el Inspector
[System.Serializable]
public class ARTarget
{
    public string targetName;        // Nombre (ej: "Cofre de Oro", "Estatua Antigua")
    public double latitude;          // Latitud exacta del sitio
    public double longitude;         // Longitud exacta del sitio
    public GameObject prefab;        // El modelo 3D con Tag "ARObject" y Collider
    [HideInInspector] public bool isDiscovered = false;
}

public class ARGPS : MonoBehaviour
{
    [SerializeField] private XROrigin sessionOrigin;

    [Header("Lista de Sitios y Reliquias")]
    [SerializeField] private ARTarget[] arTargets;

    [Header("Interfaz UI")]
    [SerializeField] private Text distanceText;
    [SerializeField] private Text messageText;

    [Header("Distancias")]
    [SerializeField] private float discoveryDistance = 15f;
    [SerializeField] private float spawnDistance = 2f;
    [SerializeField] private float yOffset = 0.05f;

    private bool gpsEnabled = false;

    private void Awake()
    {
        if (sessionOrigin == null)
            sessionOrigin = FindAnyObjectByType<XROrigin>();

        if (!Application.isEditor)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                Permission.RequestUserPermission(Permission.Camera);
        }

        // Ocultar todos los prefabs al iniciar
        //foreach (var target in arTargets)
        //{
         //   if (target.prefab != null)
        //        target.prefab.SetActive(false);
        //}

        if (messageText != null)
            messageText.text = "Buscando reliquias...";
    }

    private void Start()
    {
        Debug.Log("ARGPS Start ejecutado");
        if (messageText != null) messageText.text = "HOLA DESDE EL SCRIPT";
        StartCoroutine(UpdateGPS());
    }

    private IEnumerator UpdateGPS()
    {
        yield return new WaitForSeconds(1f);

        if (!Input.location.isEnabledByUser)
        {
            if (messageText != null) messageText.text = "Activa el GPS del dispositivo";
            yield break;
        }

        Input.location.Start(10f, 1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        if (maxWait <= 0 || Input.location.status == LocationServiceStatus.Failed)
        {
            if (messageText != null) messageText.text = "Error de señal GPS.";
            yield break;
        }

        gpsEnabled = true;

        // Bucle continuo de escaneo
        while (gpsEnabled)
        {
            double currentLat = Input.location.lastData.latitude;
            double currentLon = Input.location.lastData.longitude;

            float closestDistance = float.MaxValue;
            string closestName = "";

            // Revisar la distancia de cada objeto en la lista
            for (int i = 0; i < arTargets.Length; i++)
            {
                if (arTargets[i].isDiscovered) continue;

                float distance = CalculateDistance(currentLat, currentLon, arTargets[i].latitude, arTargets[i].longitude);

                // Registrar el más cercano para mostrarlo en el texto
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestName = arTargets[i].targetName;
                }

                // Si se entra en el rango, descubrir el objeto
                if (distance <= discoveryDistance)
                {
                    DiscoverRelic(arTargets[i], distance);
                }
            }

            // Actualizar la pantalla con el objetivo activo más cercano
            if (distanceText != null && closestDistance != float.MaxValue)
            {
                distanceText.text = $"Próximo objetivo ({closestName}): {closestDistance.ToString("F1")} m";
            }
            else if (closestDistance == float.MaxValue && distanceText != null)
            {
                distanceText.text = "¡Has encontrado todas las reliquias!";
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double earthRadius = 6371000.0;
        double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        double dLon = (lon2 - lon1) * Mathf.Deg2Rad;

        double a = Mathf.Sin((float)(dLat / 2.0)) * Mathf.Sin((float)(dLat / 2.0))
            + Mathf.Cos((float)(lat1 * Mathf.Deg2Rad)) * Mathf.Cos((float)(lat2 * Mathf.Deg2Rad))
            * Mathf.Sin((float)(dLon / 2.0)) * Mathf.Sin((float)(dLon / 2.0));

        double c = 2.0 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1.0 - a)));

        return (float)(earthRadius * c);
    }

    private void DiscoverRelic(ARTarget target, float distance)
    {
        target.isDiscovered = true;

        if (messageText != null)
            messageText.text = $"¡{target.targetName.ToUpper()} ENCONTRADA!";

        if (target.prefab == null || sessionOrigin == null)
        {
            Debug.LogError("Prefab o sessionOrigin es null");
            return;
        }

        Transform cam = sessionOrigin.Camera.transform;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        // 2 m delante de la CÁMARA, a la altura del suelo de la sesión
        Vector3 spawnPosition = cam.position + forward * spawnDistance;
        spawnPosition.y = sessionOrigin.transform.position.y + yOffset;

        GameObject instance = Instantiate(target.prefab, spawnPosition, Quaternion.LookRotation(forward));
        instance.SetActive(true);

        Debug.Log($"Spawn {target.targetName} en {spawnPosition}, cámara en {cam.position}");

        Animator anim = instance.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Appear");

        GameObject test = GameObject.CreatePrimitive(PrimitiveType.Cube);
        test.transform.position = spawnPosition + Vector3.up * 0.25f;
        test.transform.localScale = Vector3.one * 0.5f;
        test.GetComponent<Renderer>().material.color = Color.red;
    }

    private void OnDisable()
    {
        if (gpsEnabled)
        {
            Input.location.Stop();
            gpsEnabled = false;
        }
    }
}