using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using System.Text;
using System.Reflection;

public abstract class MoveSO : ScriptableObject
{
    public MoveType moveType;
    public Sprite sprite;
    public int AP = 1;
    public Scale height;
    public List<EffectChance> effects;
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(name);

      var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

      foreach (var field in fields)
      {
        if (field.Name == "sprite") continue;

        object value = field.GetValue(this);

        if (value is IEnumerable list && !(value is string))
        {
            List<string> items = new List<string>();

            foreach (var item in list)
            {
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
