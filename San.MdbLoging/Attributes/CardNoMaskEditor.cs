using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MongoLogger;
using System.Reflection;

namespace MongoLogger.Attributes
{
    public class CardNoMaskEditor
    {
        public void Edit(object instance,ref object editdObj)
        {
            var propsWithMaskAttrs = instance.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(CardNoMaskAttribute)));
                        editdObj = instance.Clone();
            foreach (var pi in propsWithMaskAttrs)
            {
                var editdObjCast = Convert.ChangeType(editdObj, instance.GetType());
                var piCopy = editdObj.GetType().GetType().GetProperty(pi.Name);
                
                                                                            }
        }

        private object mask(object value)
        {
            if (value == null)
                return null;

            var valStr = value.ToString();
            if (valStr.Length != 16)
                return value;

            var firstPart = valStr.Substring(0, 6);
            var lastPart = valStr.Substring(12, 4);
            var convertedVal = firstPart + "******" + lastPart;
            return convertedVal;
        }

        private void SetProperty(string compoundProperty, object target, object value)
        {
            string[] bits = compoundProperty.Split('.');
            for (int i = 0; i < bits.Length - 1; i++)
            {
                PropertyInfo propertyToGet = target.GetType().GetProperty(bits[i]);
                target = propertyToGet.GetValue(target, null);
            }
            PropertyInfo propertyToSet = target.GetType().GetType().GetProperty(bits.Last());
            propertyToSet.SetValue(target, value, null);
        }
    }
}
