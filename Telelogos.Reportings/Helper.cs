using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Telelogos.Reportings
{
   public static class Helper
   {
      // Copy the first object fields values to the second one
      public static void ShallowCopyValues<T1, T2>(T1 firstObject, T2 secondObject)
      {
         const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
         var firstFieldDefinitions = firstObject.GetType().GetFields(bindingFlags);
         var secondFieldDefinitions = secondObject.GetType().GetFields(bindingFlags);

         foreach (var fieldDefinition in firstFieldDefinitions)
         {
            var matchingFieldDefinition = secondFieldDefinitions.FirstOrDefault(fd => fd.Name == fieldDefinition.Name &&
                                                                                      fd.FieldType == fieldDefinition.FieldType);
            if (matchingFieldDefinition == null)
               continue;

            var value = fieldDefinition.GetValue(firstObject);
            matchingFieldDefinition.SetValue(secondObject, value);
         }
      }
   }
}
