using System.Collections.Generic;
using System.Collections;

using System.Reflection;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Item")]
public class ItemSO : ScriptableObject
{
    public Sprite sprite;
    public List<ItemEffect> effects;
    
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(name);

      var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

      foreach (var field in fields)
      {
        object value = field.GetValue(this);

        // Handle lists nicely
        if (value is IEnumerable list && !(value is string))
        {
            List<string> items = new List<string>();

            foreach (var item in list)
            {
                items.Add(item.ToString().Replace("I", "Increase "));
            }

            sb.AppendLine($"{field.Name}: {string.Join(", ", items)}");
        }
       }

        return sb.ToString();
   }
}
