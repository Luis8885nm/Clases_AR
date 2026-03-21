using UnityEngine;
using Vuforia;
using System.Collections;

public class NarradorPorschePro : MonoBehaviour
{
    [Header("Referencias de Movimiento")]
    public ObserverBehaviour[] marcadores;
    public float velocidad = 2.0f;
    private bool seEstaMoviendo = false;
    private int indiceMeta = -1;

    [Header("Referencias Estéticas")]
    // Lo dejamos vacío en el inspector, el código lo buscará solo
    private Renderer mallaAuto;
    public GameObject[] accesorios;
    public ParticleSystem particulas;
    public AudioSource sonido;
    private Color colorOriginal;

    void Start()
    {
        // --- AQUÍ ESTÁ EL TRUCO DE INGENIERÍA ---
        // Buscamos el objeto por su nombre exacto en los hijos
        GameObject piezaCarroceria = GameObject.Find("body_all_body_main_0_4");

        if (piezaCarroceria != null)
        {
            mallaAuto = piezaCarroceria.GetComponent<Renderer>();
            if (mallaAuto != null)
            {
                colorOriginal = mallaAuto.material.color;
                Debug.Log("¡Carrocería encontrada y asignada automáticamente!");
            }
        }
        else
        {
            Debug.LogError("No se encontró el objeto 'body_all_body_main_0_4'. Revisa que el nombre sea exacto.");
        }

        // Apagar accesorios
        foreach (GameObject obj in accesorios) if (obj != null) obj.SetActive(false);
    }

    public void IrAlSiguientePaso()
    {
        if (!seEstaMoviendo) StartCoroutine(MoverAuto());
    }

    IEnumerator MoverAuto()
    {
        Transform objetivo = null;
        int idMarcadorLlegada = -1;

        for (int i = 0; i < marcadores.Length; i++)
        {
            if (marcadores[i].TargetStatus.Status == Status.TRACKED ||
                marcadores[i].TargetStatus.Status == Status.EXTENDED_TRACKED)
            {
                if (i != indiceMeta && Vector3.Distance(transform.position, marcadores[i].transform.position) > 0.1f)
                {
                    objetivo = marcadores[i].transform;
                    idMarcadorLlegada = i;
                    break;
                }
            }
        }

        if (objetivo == null) yield break;

        seEstaMoviendo = true;
        float tiempo = 0;
        Vector3 posInicial = transform.position;
        Quaternion rotInicial = transform.rotation;

        while (tiempo < 1.0f)
        {
            tiempo += Time.deltaTime * velocidad;
            transform.position = Vector3.Lerp(posInicial, objetivo.position, tiempo);
            transform.rotation = Quaternion.Lerp(rotInicial, objetivo.rotation, tiempo);
            yield return null;
        }

        indiceMeta = idMarcadorLlegada;
        if (particulas != null) { particulas.transform.position = transform.position; particulas.Play(); }
        if (sonido != null) sonido.Play();

        EjecutarAccion(idMarcadorLlegada);
        seEstaMoviendo = false;
    }

    void EjecutarAccion(int id)
    {
        if (mallaAuto == null) return;

        switch (id)
        {
            case 1:
                foreach (GameObject obj in accesorios) if (obj != null) obj.SetActive(false);
                int azar = Random.Range(0, accesorios.Length);
                if (accesorios[azar] != null) accesorios[azar].SetActive(true);
                break;
            case 2: mallaAuto.material.color = Color.red; break;
            case 3: mallaAuto.material.color = colorOriginal; break;
        }
    }
}