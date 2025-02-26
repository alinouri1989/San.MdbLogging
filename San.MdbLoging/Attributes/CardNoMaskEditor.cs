using San.MdbLogging.Attributes;
using System.Reflection;

namespace San.MdbLogging.Attributes;

public class CardNoMaskEditor
{
    public void Edit(object instance, ref object editdObj)
    {
        IEnumerable<PropertyInfo> enumerable = from pi in instance.GetType().GetProperties()
                                               where Attribute.IsDefined(pi, typeof(CardNoMaskAttribute))
                                               select pi;
        editdObj = instance.Clone();
        foreach (PropertyInfo item in enumerable)
        {
            Convert.ChangeType(editdObj, instance.GetType());
            editdObj.GetType().GetType().GetProperty(item.Name);
        }
    }

    private object mask(object value)
    {
        if (value == null)
        {
            return null;
        }

        string text = value.ToString();
        return (text.Length != 16) ? value : (text.Substring(0, 6) + "******" + text.Substring(12, 4));
    }

    private void SetProperty(string compoundProperty, object target, object value)
    {
        string[] array = compoundProperty.Split('.');
        for (int i = 0; i < array.Length - 1; i++)
        {
            target = target.GetType().GetProperty(array[i]).GetValue(target, null);
        }

        target.GetType().GetType().GetProperty(array.Last())
            .SetValue(target, value, null);
    }
}