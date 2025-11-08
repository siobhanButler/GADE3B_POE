using UnityEngine;

public class Loot
{
    public int lootID;
    public string lootName;
    public int lootCost;
    public float lootLikelihood;
    public LootRarity lootRarity;
    public LootProperty lootProperty;
    public Sprite lootSprite;
    public Color lootColor;     //corresponds to the rarity

    public Loot(LootItem lootItem, LootProperty property)
    {
        this.lootID = lootItem.lootID;
        this.lootName = lootItem.lootName;
        this.lootCost = lootItem.lootCost;
        this.lootLikelihood = lootItem.lootBaseLikelihood;
        this.lootRarity = lootItem.lootRarity;
        this.lootSprite = lootItem.lootSprite;
        this.lootColor = lootItem.lootColor;
        this.lootProperty = property;

        SetColour();
    }

    void SetColour()
    {
        switch (lootRarity)
        {
            case LootRarity.Common:
                lootColor = Color.white;
                break;
            case LootRarity.Uncommon:
                lootColor = Color.green;
                break;
            case LootRarity.Rare:
                lootColor = Color.blue;
                break;
            case LootRarity.Legendary:
                lootColor = Color.gold;
                break;
            case LootRarity.Mythic:
                lootColor = Color.purple;
                break;
        }
    }
}
