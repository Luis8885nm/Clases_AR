using UnityEngine;

public class ControladorColor : MonoBehaviour
{
    // arrastrar la esfera 'body_main' de la carpeta Materials
    public Material materialCuerpo;

    public void CambiarAzul()
    {
        //  cambia el color del auto a azul
        materialCuerpo.color = Color.blue;
    }
}