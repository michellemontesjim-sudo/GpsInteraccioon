using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using System.Collections;



public class ARGPS : MonoBehaviour

{
    [SerializeField] private XROrigin sessionOrigin;
    [SerializeField] private GameObject prefab;

    [SerializeField] private Text distanceText;
    [SerializeField] private Text messageText;

    [SerializeField] private double targetLatitude;
    [SerializeField] private double targetLongitude;

    [SerializeField] private float discoveryDistance = 15f;
    [SerializeField] private float spawnDistance = 2f;

    [SerializeField] private float yOffset = 0.05f;

    private bool gpsEnabled = false;
    private bool relicDiscovered = false;

    public double latitude;
    public double longitude;
    public double altitude;

    public void Awake()
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

        if (prefab != null)
            prefab.SetActive(false);
        if (messageText != null)
            messageText.text = "Busca la reliquia...";
    }


    private void Start()
    {
        StartCoroutine(UpdateGPS());
    }

    private IEnumerator UpdateGPS()
    {
        yield return new WaitForSeconds(1f);

        if (!Input.location.isEnabledByUser)
        {
            if (messageText != null)
                messageText.text = "Activa la ubicación del dispositivo";

            yield break;
        }

        Input.location.Start(10f, 1f);

        int maxWait = 20;

        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            if (messageText != null)
                messageText.text = "Tiempo agotado iniciando GPS.";

            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            if (messageText != null) messageText.text = "No se pudo obtener la ubicación";
            yield break;
        }

        gpsEnabled = true;

        while (gpsEnabled && !relicDiscovered)
        {
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
            altitude = Input.location.lastData.altitude;

            float distance = CalculateDistance(latitude, longitude, targetLatitude, targetLongitude);

            if (distanceText != null)
                distanceText.text = "Distancia: " + distance.ToString("F1") + " m";

            if (distance <= discoveryDistance)
                DiscoveryRelic(distance);

            yield return new WaitForSeconds(1f);

        }

    }

    private float CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double earthRadius = 6371000.0;
        double lat1 = latitude1 * Mathf.Deg2Rad;
        double lat2 = latitude2 * Mathf.Deg2Rad;
        double deltaLat = (latitude2 - latitude1) * Mathf.Deg2Rad;
        double deltaLon = (longitude2 - longitude1) * Mathf.Deg2Rad;

        double a = Mathf.Sin((float)(deltaLat / 2.0)) * Mathf.Sin((float)(deltaLat / 2.0))
            + Mathf.Cos((float)lat1) * Mathf.Cos((float)lat2) * Mathf.Sin((float)(deltaLon / 2.0))
            * Mathf.Sin((float)(deltaLon / 2.0));

        double c = 2.0 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1.0 - a)));

        return (float)(earthRadius * c);
    }

    private void DiscoveryRelic(float distance)
    {
        relicDiscovered = true;

        if (messageText != null)
            messageText.text = "¡RELIQUIA ENCONTRADA!";

        if (prefab == null || sessionOrigin == null)
            return;

        Vector3 spawnPosition = sessionOrigin.transform.position + sessionOrigin.transform.forward * spawnDistance;
        spawnPosition.y += yOffset;

        Vector3 forward = sessionOrigin.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            prefab.transform.rotation = Quaternion.LookRotation(forward);

        prefab.transform.position = spawnPosition;
        prefab.SetActive(true);

        Input.location.Stop();
        gpsEnabled = false;

        Debug.Log("Reliquia encontrada a " + distance.ToString("F1") + " metros.");
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
