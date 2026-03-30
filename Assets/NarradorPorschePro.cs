using UnityEngine;
using Vuforia;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class NarradorPorschePro : MonoBehaviour
{
    public enum TipoAccion { Taller, Pista, Gasolina, Garage, Meta }

    [Header("Movimiento")]
    public ObserverBehaviour[] marcadores;
    public float velocidad = 2.0f;
    private bool seEstaMoviendo = false;
    private int ultimoIndiceVisitado = -1;
    private bool esPrimerEscaneo = true;

    [Header("Escenografía (Modelos en Targets)")]
    // Arrastra aquí los modelos hijos de los targets en el mismo orden que la lista de marcadores
    public GameObject[] decoracionesTargets;

    [Header("Modelos Auto")]
    public GameObject modeloNormal;
    public GameObject modeloCarreras;
    public GameObject[] accesorios;

    [Header("Audio")]
    public AudioSource musicaFondo;
    public AudioSource sfxMotor;
    public AudioClip sonidoVictoria;
    public AudioClip sonidoLlegada;

    [Header("Interfaz UI")]
    public TextMeshProUGUI textoUI;
    public TextMeshProUGUI textoTiempo;
    public GameObject panelMenuPrincipal;
    public GameObject panelAlertaRastreo;
    public GameObject botonReiniciar;

    [Header("Lógica")]
    public float tiempoMaximo = 90f;
    private float tiempoRestante;
    private bool tieneModeloCarreras = false, tieneAleron = false, juegoTerminado = true;
    private HashSet<int> eventosVisitados = new HashSet<int>();
    private Dictionary<int, TipoAccion> mapaAcciones = new Dictionary<int, TipoAccion>();

    void Start()
    {
        panelMenuPrincipal.SetActive(true);
        juegoTerminado = true;
    }

    public void IniciarJuego()
    { // Conecta esto al botón "Iniciar Carrera"
        panelMenuPrincipal.SetActive(false);
        juegoTerminado = false;
        botonReiniciar.SetActive(true);
        tieneModeloCarreras = false;
        tieneAleron = false;
        modeloNormal.SetActive(true);
        modeloCarreras.SetActive(false);
        foreach (GameObject acc in accesorios) if (acc) acc.SetActive(false);
        tiempoRestante = tiempoMaximo;
        eventosVisitados.Clear();
        ultimoIndiceVisitado = -1;
        esPrimerEscaneo = true;

        AsignarAccionesAleatorias();
        if (musicaFondo) musicaFondo.Play();
        foreach (GameObject deco in decoracionesTargets) if (deco) deco.SetActive(false);
    }

    void Update()
    {
        if (juegoTerminado) return;

        tiempoRestante -= Time.deltaTime;
        if (textoTiempo) textoTiempo.text = "TIEMPO: " + Mathf.Max(0, (int)tiempoRestante) + "s";

        if (tiempoRestante <= 0) FinalizarJuego(false, "¡TIEMPO AGOTADO!");

        ManejarRastreoPerdido();
        if (!seEstaMoviendo) VerificarMarcadores();
    }

    void ManejarRastreoPerdido()
    {
        if (ultimoIndiceVisitado != -1 && panelAlertaRastreo)
        {
            var status = marcadores[ultimoIndiceVisitado].TargetStatus.Status;

            // RIGIDEZ: Solo aceptamos TRACKED (vista directa al papel). 
            // Ignoramos EXTENDED_TRACKED y LIMITED.
            bool estaViendo = (status == Status.TRACKED);

            // 1. Mostramos/Ocultamos el mensaje de alerta
            panelAlertaRastreo.SetActive(!estaViendo);

            // 2. APAGAR EL COCHE: Si no hay papel, el coche es invisible.
            // Esto evita que se quede flotando en el aire.
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = estaViendo;
            }
        }
    }

    void VerificarMarcadores()
    {
        for (int i = 0; i < marcadores.Length; i++)
        {
            if (marcadores[i].TargetStatus.Status == Status.TRACKED)
            {
                if (i != ultimoIndiceVisitado)
                {
                    StartCoroutine(MoverAuto(i));
                    break;
                }
            }
        }
    }
    IEnumerator MoverAuto(int indice)
    {
        seEstaMoviendo = true;
        textoUI.text = "VIAJANDO AL OBJETIVO...";

        // 1. Limpieza del Memorama (Apagar la decoración anterior)
        if (ultimoIndiceVisitado != -1)
        {
            TipoAccion accionAnterior = mapaAcciones[ultimoIndiceVisitado];
            int indiceViejo = (int)accionAnterior;
            if (decoracionesTargets[indiceViejo])
                decoracionesTargets[indiceViejo].SetActive(false);
        }

        // 2. Iniciamos Movimiento
        transform.SetParent(null);
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;

        if (!esPrimerEscaneo)
        {
            float t = 0;
            Vector3 pIni = transform.position; Quaternion rIni = transform.rotation;
            while (t < 1.0f)
            {
                t += Time.deltaTime * velocidad;
                transform.position = Vector3.Lerp(pIni, marcadores[indice].transform.position, t);
                transform.rotation = Quaternion.Lerp(rIni, marcadores[indice].transform.rotation, t);
                yield return null;
            }
        }
        else
        {
            // --- RIGIDEZ DE INGENIERÍA: SNAP PERFECTO EN EL PRIMER ESCANEO ---
            // 1. Emparentamos primero
            transform.SetParent(marcadores[indice].transform);

            // 2. Reseteamos coordenadas LOCALES a cero absoluto (0,0,0)
            transform.localPosition = Vector3.zero;

            // 3. Forzamos rotación local de 180 para que el coche te mire
            transform.localRotation = Quaternion.Euler(0, 180f, 0);

            esPrimerEscaneo = false;
        }

        // 3. LLEGADA Y DESCUBRIMIENTO
        // (Asegurar el emparentado final para los movimientos Lerp)
        transform.SetParent(marcadores[indice].transform);
        ultimoIndiceVisitado = indice;
        eventosVisitados.Add(indice);

        TipoAccion accionActual = mapaAcciones[indice];
        int indiceDecoracion = (int)accionActual;

        if (decoracionesTargets[indiceDecoracion])
        {
            decoracionesTargets[indiceDecoracion].SetActive(true);
            decoracionesTargets[indiceDecoracion].transform.SetParent(marcadores[indice].transform);
            decoracionesTargets[indiceDecoracion].transform.localPosition = new Vector3(0, 0.05f, 0);
            decoracionesTargets[indiceDecoracion].transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
        }

        if (sfxMotor && sonidoLlegada) sfxMotor.PlayOneShot(sonidoLlegada);
        ProcesarAccionAleatoria(accionActual);

        yield return new WaitForSeconds(1.0f);
        seEstaMoviendo = false;
    }

    void ProcesarAccionAleatoria(TipoAccion accion)
    {
        switch (accion)
        {
            case TipoAccion.Taller: tieneAleron = true; accesorios[1].SetActive(true); textoUI.text = "¡AERO MEJORADA!"; break;
            case TipoAccion.Pista:
                // Solo permitimos la conversión GT3 RS si no es el primer stop
                if (eventosVisitados.Count > 1)
                {
                    tieneModeloCarreras = true;
                    modeloNormal.SetActive(false);
                    modeloCarreras.SetActive(true);
                    textoUI.text = "¡CONVERSIÓN GT3 RS COMPLETA!";
                    // encender un accesorio de carreras aquí
                    accesorios[1].SetActive(true); // Enciende el Alerón
                }
                else
                {
                    // Si caes aquí en el primer stop, el coche no cambia, obligando a buscar más.
                    textoUI.text = "¡PISTA LOCALIZADA! (Falta Taller)";
                }
                break;
            case TipoAccion.Gasolina: textoUI.text = "¡CARGA COMPLETADA!"; break;
            case TipoAccion.Garage: textoUI.text = "¡NEUMÁTICOS LISTOS!"; break;
            case TipoAccion.Meta:
                if (eventosVisitados.Count >= 5 && tieneModeloCarreras && tieneAleron) FinalizarJuego(true, "¡VICTORIA!");
                else textoUI.text = "¡META! Te faltan objetivos."; break;
        }
    }

    void FinalizarJuego(bool victoria, string msj)
    {
        juegoTerminado = true;
        textoUI.text = msj;
        botonReiniciar.SetActive(true);
        if (victoria && sonidoVictoria) sfxMotor.PlayOneShot(sonidoVictoria);
    }

    void AsignarAccionesAleatorias()
    {
        List<TipoAccion> accs = new List<TipoAccion> { TipoAccion.Taller, TipoAccion.Pista, TipoAccion.Gasolina, TipoAccion.Garage, TipoAccion.Meta };
        for (int i = 0; i < accs.Count; i++)
        {
            TipoAccion tmp = accs[i]; int r = Random.Range(i, accs.Count);
            accs[i] = accs[r]; accs[r] = tmp;
        }
        for (int i = 0; i < marcadores.Length; i++) mapaAcciones[i] = accs[i];
    }

    public void Reiniciar() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
}