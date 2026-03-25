using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using System.Text;
using System.Reflection;
using System.Linq;

[CreateAssetMenu(menuName = "ScriptableObjects/Character")]
public class CharacterSO : ScriptableObject
{
    public Sprite sprite;
    public int hitPoints;
    public int attack;
    public float accuracy;
    public float evasion;
    public List<AttackSO> aMoves;
    public List<DefendSO> dMoves;

    public int actionPoints;
    
    [Header("Computer specific attributes")]
    public float defendRate = 1f;
    public List<AttackChance> attackChances;
    public List<DefendChance> defendChances;
    public bool sameMovesAsPlayer = false;
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(name);

        var excluded = new HashSet<string>{"sprite","attackChances","defendChances"};

        var fields = GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !excluded.Contains(f.Name));

        foreach (var field in fields)
        {
            object value = field.GetValue(this);

            // Handle lists nicely
            if (value is IEnumerable list && !(value is string))
            {
                List<string> items = new List<string>();

                foreach (var item in list)
                {
                    if (item is Object unityObj)
                        items.Add(unityObj.name);
                    else
                        items.Add(item?.ToString());
                }

                sb.AppendLine($"{field.Name}: {string.Join(", ", items)}");
            }
            else
            {
                sb.AppendLine($"{field.Name}: {value}");
            }
        }

        return sb.ToString();
    }
}
