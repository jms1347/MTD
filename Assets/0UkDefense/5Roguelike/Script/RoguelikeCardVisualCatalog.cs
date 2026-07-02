using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RoguelikeCardVisualCatalog", menuName = "UkDefense/Roguelike Card Visual Catalog")]
public class RoguelikeCardVisualCatalog : ScriptableObject
{
    public Sprite cardRed;
    public Sprite cardBlue;
    public Sprite cardGreen;
    public Sprite cardYellow;

    public Sprite Resolve(RoguelikeCardColor color)
    {
        return color switch
        {
            RoguelikeCardColor.Blue => cardBlue != null ? cardBlue : cardRed,
            RoguelikeCardColor.Green => cardGreen != null ? cardGreen : cardRed,
            RoguelikeCardColor.Yellow => cardYellow != null ? cardYellow : cardRed,
            _ => cardRed
        };
    }
}
