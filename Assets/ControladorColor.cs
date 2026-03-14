using UnityEngine;

public class ControladorColor : MonoBehaviour
{
    [Header("Carrocería")]
    public Material materialCuerpo;
    private Color colorOriginal;
    private bool esRojo = false;

    [Header("Accesorios")]
    // Aquí arrastra: 0-Gorra, 1-Alerón, 2-Luces
    public GameObject[] accesorios;
    // Aquí arrastra los materiales individuales de esos 3
    public Material[] materialesAccesorios;
    private int indiceActual = -1;

    void Start()
    {
        if (materialCuerpo != null) colorOriginal = materialCuerpo.color;
    }

    // BOTÓN 1: Alternar Color del Porsche (Naranja <-> Rojo)
    public void AlternarColorAuto()
    {
        esRojo = !esRojo;
        if (materialCuerpo != null)
            materialCuerpo.color = esRojo ? Color.red : colorOriginal;
    }

    // BOTÓN 2: Aparecer/Cambiar accesorio al azar
    public void CambiarObjetoAleatorio()
    {
        // Apagamos el que esté visible actualmente
        if (indiceActual != -1) accesorios[indiceActual].SetActive(false);

        // Elegimos un nuevo índice al azar entre 0 y 2
        indiceActual = Random.Range(0, accesorios.Length);
        accesorios[indiceActual].SetActive(true);
    }

    // BOTÓN 3: Cambiar color del accesorio que esté visible
    public void CambiarColorObjeto()
    {
        if (indiceActual != -1 && materialesAccesorios[indiceActual] != null)
        {
            // Asigna un color aleatorio para mayor interactividad
            materialesAccesorios[indiceActual].color = new Color(Random.value, Random.value, Random.value);
        }
    }
}