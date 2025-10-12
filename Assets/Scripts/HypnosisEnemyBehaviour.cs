using System.Collections.Generic;
using UnityEngine;

public class HypnosisEnemyBehaviour : CustomBehaviour
{
    //[SerializeField] private float hypnosisRang;                //range of the hypnosis ability
    //[SerializeField] private SphereCollider hypnosisCollider;   //collider used to detect players in range of hypnosis

    [SerializeField] [Range (0.0f, 1.0f)] private float healthThreshold;   //When health falls below this value (%) it is susceptible to hypnosis
    private ObjectManager hypnotizedTarget;                                //the target that is being hypnotized
    private string originalTag;                                            //the original tag of the target before being hypnotized
    private List<string> originalAttackableTags;                           //the original attackable tags of the target before being hypnotized
    private Color originalColor;                                            //the original color of the target before being hypnotized

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected override void AttackBehaviour(ObjectManager target, float damage)
    {
        if (hypnotizedTarget != null) return;    //if already hypnotizing a target, do nothing
        if ((target.health.currentHealth/target.health.maxHealth) <= healthThreshold && target.tag != "MainTower")    //if remaining target health is below the threshold and not the main tower
        {
            Debug.Log($"{gameObject.name} is hypnotizing {target.gameObject.name}");
            //Hypnotize Target
            hypnotizedTarget = target;                     //set hypnotized target
            originalTag = target.tag;                      //store original tag
            originalAttackableTags = new List<string>(target.attack.attackableTags); //store original attackable tags
            originalColor = hypnotizedTarget.GetComponent<MeshRenderer>().material.color; //store original color

            hypnotizedTarget.tag = "Enemy";                           //change target tag to enemy, so that it can be attacked by towers
            hypnotizedTarget.attack.attackableTags.Remove("Enemy");   //clear attackable tags
            hypnotizedTarget.attack.attackableTags.Add("DefenceTower");      //add tower tag to attackable tags, so that it attacks other towers
            hypnotizedTarget.attack.StopAttack();
            hypnotizedTarget.attack.RefreshAttack();                //refresh the attack due to change in attackable targets

            this.GetComponent<MeshRenderer>().material.color = Color.cyan; //change color to cyan to indicate hypnosis
            hypnotizedTarget.GetComponent<BoxCollider>().enabled = false;
            hypnotizedTarget.GetComponent<BoxCollider>().enabled = true;

            hypnotizedTarget.GetComponent<MeshRenderer>().material.color = Color.red; //change color to magenta to indicate hypnosis
        }
    }

    protected override void DamageBehaviour(ObjectManager target, float damage)
    {
        throw new System.NotImplementedException();
    }

    void OnDestroy()
    {
        //if this enemy is destroyed (aka dies) while hypnotizing a target, un-hypnotize the target
        if (hypnotizedTarget != null)               //if there is a target/ it hasnt been destroyed
        {
            hypnotizedTarget.tag = originalTag;     //change target tag back to player
            hypnotizedTarget.attack.attackableTags = originalAttackableTags; //restore original attackable tags
            hypnotizedTarget.GetComponent<MeshRenderer>().material.color = originalColor; //restore original color
            hypnotizedTarget.attack.RefreshAttack();                                 //refresh attackable targets
            hypnotizedTarget.GetComponent<BoxCollider>().enabled = false;
            hypnotizedTarget.GetComponent<BoxCollider>().enabled = true;
            Debug.Log($"{gameObject.name} is un-hypnotizing {hypnotizedTarget.gameObject.name} due to destruction");
        }
    }
}
