using UnityEngine;

[CreateAssetMenu(fileName = "LootItem", menuName = "Scriptable Objects/LootItem")]
public class LootItem : ScriptableObject
{
    public int lootID;
    public string lootName;
    public int lootCost;
    public float lootBaseLikelihood;
    public LootRarity lootRarity;
    public Sprite lootSprite;
    public Color lootColor;     //corresponds to the rarity

    private void OnValidate()
    {
        SetLikelihood();
        lootID = lootName.GetHashCode();
    }

    void SetLikelihood() 
    { 
        switch (lootRarity)
        {
            case LootRarity.Common:
                lootBaseLikelihood = 1f;
                break;
            case LootRarity.Uncommon:
                lootBaseLikelihood = 0.8f;
                break;
            case LootRarity.Rare:
                lootBaseLikelihood = 0.5f;
                break;
            case LootRarity.Legendary:
                lootBaseLikelihood = 0.3f;
                break;
            case LootRarity.Mythic:
                lootBaseLikelihood = 0.1f;
                break;
        }
    }
}

public enum LootRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
    Mythic
}

public enum LootProperty
{
    Health = 0,
    AttackDamage = 1,
    AttackSpeed = 2,
    AttackRange = 3
}
