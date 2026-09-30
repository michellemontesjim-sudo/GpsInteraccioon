using UnityEngine;

public class LogEnPantalla : MonoBehaviour
{
    private string log = "";

    void OnEnable() { Application.logMessageReceived += Capturar; }
    void OnDisable() { Application.logMessageReceived -= Capturar; }

    void Capturar(string mensaje, string stack, LogType tipo)
    {
        if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Warning || tipo == LogType.Log)
        {
            log += $"[{tipo}] {mensaje}\n";
            if (log.Length > 1500) log = log.Substring(log.Length - 1500);
        }
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 28;
        GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height / 2), log);
    }
}