using UnityEngine;
using System.Collections.Generic;

public class PathManager
{
    public SpawnerManager spawner;
    public List<SubCell> path;

    public List<ObjectManager> towers;

    //Per Wave Basis
    public float TotalTowerDamage;
    public float TotalTowerHealth;
    public float TotalEnemyDamage;
    public float TotalEnemyHealth;

    public float completionTime;
}
