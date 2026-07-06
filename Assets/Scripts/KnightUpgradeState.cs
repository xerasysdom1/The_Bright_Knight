using UnityEngine;

public class KnightUpgradeState : MonoBehaviour
{
    [SerializeField] bool hasSparkSpell;
    [SerializeField] bool hasRadiantGuardSpell;

    public bool HasSparkSpell => hasSparkSpell;
    public bool HasRadiantGuardSpell => hasRadiantGuardSpell;

    public bool LearnSparkSpell()
    {
        if (hasSparkSpell)
            return false;

        hasSparkSpell = true;
        return true;
    }

    public bool LearnRadiantGuardSpell()
    {
        if (hasRadiantGuardSpell)
            return false;

        hasRadiantGuardSpell = true;
        return true;
    }
}
