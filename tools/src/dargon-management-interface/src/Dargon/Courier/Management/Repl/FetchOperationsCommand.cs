﻿using System;
using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
   public class FetchOperationsCommand : ICommand {
      public string Name => "fetch-ops";

      public int Eval(string args) {
         var mob = ReplGlobals.Current?.MobDto;
         if (mob == null) {
            throw new Exception("Mob not specified.");
         }
         var desc = ReplGlobals.ManagementObjectService.GetManagementObjectDescription(mob.Id);
         foreach (var method in desc.Methods) {
            var methodNode = ReplGlobals.Current.GetOrAddChild(method.Name);
            methodNode.MethodDto = method;
         }
         foreach (var property in desc.Properties) {
            var propertyNode = ReplGlobals.Current.GetOrAddChild(property.Name);
            propertyNode.PropertyDto = property;
         }
         foreach (var dataSet in desc.DataSets) {
            var dataSetNode = ReplGlobals.Current.GetOrAddChild(dataSet.Name);
            dataSetNode.DataSetDto = dataSet;
         }

         Console.WriteLine("Fetched management object operations.");
         return 0;
      }
   }
}
