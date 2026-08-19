using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.Text;

// Este componente va sobre el prefab "Card" que se instancia por cada carta
// del mazo del usuario. El prefab debe tener:
//  - Un RawImage (o Image) para el retrato del personaje
//  - Un TMP_Text para el nombre
//  - Un TMP_Text para la especie
public class CardView : MonoBehaviour
{
    [SerializeField] private RawImage cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text speciesText;
    [SerializeField] private GameObject loadingSpinner; // opcional, se apaga cuando llega la imagen

    public void SetTextData(Character character)
    {
        nameText.text = character.name;
        speciesText.text = character.species;
    }

    public void SetImage(Texture2D texture)
    {
        if (cardImage != null && texture != null)
        {
            cardImage.texture = texture;
        }
        if (loadingSpinner != null)
        {
            loadingSpinner.SetActive(false);
        }
    }
}